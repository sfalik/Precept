#!/usr/bin/env node
// squad-review.js — Post GitHub PR reviews as squad-reviewer[bot]
//
// Usage (initial review — inline comments on specific files/lines):
//   node tools/scripts/squad-review.js <pr-number> <review-file.json>
//   JSON: { "event": "REQUEST_CHANGES", "body": "...", "comments": [...] }
//
// Usage (reply — respond to existing threads, optionally resolve):
//   node tools/scripts/squad-review.js <pr-number> <reply-file.json>
//   JSON: { "event"?: "APPROVE", "body"?: "...", "replies": [...] }
//
// Usage (list threads — show existing review threads for mapping):
//   node tools/scripts/squad-review.js <pr-number> threads [--unresolved]
//
// Usage (simple — markdown body only, legacy):
//   node tools/scripts/squad-review.js <pr-number> <event> <body-file.md>
//
// Review JSON (initial review with inline comments):
//   {
//     "event": "REQUEST_CHANGES",
//     "body": "## Overall review summary...",
//     "comments": [
//       { "path": "src/File.cs", "line": 42, "body": "**B1:** ..." }
//     ]
//   }
//
// Reply JSON (respond to existing threads):
//   {
//     "event": "APPROVE",              // optional — submit a review alongside replies
//     "body": "## Re-review summary",  // optional — review body
//     "replies": [
//       { "commentId": 123456, "body": "Fixed in commit abc123.", "resolve": true }
//     ]
//   }
//
//   event/body are optional in reply mode. If present, a review is submitted.
//   If only replies are present, just the replies are posted (no review event).
//   resolve: true resolves the thread after replying (reviewer use only).
//
// Environment variables (optional overrides):
//   SQUAD_REVIEWER_APP_ID          — GitHub App ID (default: from config)
//   SQUAD_REVIEWER_INSTALLATION_ID — Installation ID (default: from config)
//   SQUAD_REVIEWER_PEM_PATH        — Path to .pem file (default: ~/.squad-reviewer/private-key.pem)
//   SQUAD_REVIEWER_OWNER           — Repo owner (default: sfalik)
//   SQUAD_REVIEWER_REPO            — Repo name (default: Precept)

import { createSign } from "node:crypto";
import { readFileSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { homedir, tmpdir } from "node:os";
import { spawnSync } from "node:child_process";

// --- Config (override via env vars) ---
const APP_ID = process.env.SQUAD_REVIEWER_APP_ID || "3355906";
const INSTALLATION_ID = process.env.SQUAD_REVIEWER_INSTALLATION_ID || "123390393";
const PEM_PATH = process.env.SQUAD_REVIEWER_PEM_PATH || resolve(homedir(), ".squad-reviewer", "private-key.pem");
const OWNER = process.env.SQUAD_REVIEWER_OWNER || "sfalik";
const REPO = process.env.SQUAD_REVIEWER_REPO || "Precept";

// --- CLI args ---
const args = process.argv.slice(2);

let prNumber, reviewEvent, reviewBody, reviewComments, reviewReplies;
let mode = "review"; // "review" | "threads" | "reply"

if (args.length >= 2 && args[1] === "threads") {
  // Threads mode: <pr-number> threads [--unresolved]
  mode = "threads";
  prNumber = args[0];
  const unresolvedOnly = args.includes("--unresolved");

  // Executed in main block below
  var threadsUnresolvedOnly = unresolvedOnly;
} else if (args.length === 2) {
  // Structured mode: <pr-number> <file.json>
  prNumber = args[0];
  const filePath = args[1];
  const content = readFileSync(resolve(filePath), "utf8");

  let parsed;
  try {
    parsed = JSON.parse(content);
  } catch {
    console.error("Structured mode requires a valid JSON file. Got parse error.");
    console.error("Usage: node squad-review.js <pr-number> <review-file.json>");
    process.exit(1);
  }

  if (parsed.replies) {
    // Reply mode — respond to existing threads, optionally with new comments
    mode = "reply";
    reviewReplies = parsed.replies;
    reviewEvent = parsed.event?.toUpperCase() || null;
    reviewBody = parsed.body || null;
    reviewComments = parsed.comments || [];

    // Validate replies
    for (const [i, r] of reviewReplies.entries()) {
      if (!r.commentId || !r.body) {
        console.error(`Reply [${i}] missing required field (commentId, body).`);
        process.exit(1);
      }
      if (typeof r.commentId !== "number" || r.commentId < 1) {
        console.error(`Reply [${i}] commentId must be a positive integer. Got: ${r.commentId}`);
        process.exit(1);
      }
    }

    // Validate new comments (if any)
    for (const [i, c] of reviewComments.entries()) {
      if (!c.path || !c.line || !c.body) {
        console.error(`Comment [${i}] missing required field (path, line, body).`);
        process.exit(1);
      }
      if (typeof c.line !== "number" || c.line < 1) {
        console.error(`Comment [${i}] line must be a positive integer. Got: ${c.line}`);
        process.exit(1);
      }
    }
  } else {
    // Review mode — initial review with inline comments
    if (!parsed.event || !parsed.body) {
      console.error('JSON must contain "event" and "body" fields (for review) or "replies" array (for reply).');
      process.exit(1);
    }

    reviewEvent = parsed.event.toUpperCase();
    reviewBody = parsed.body;
    reviewComments = parsed.comments || [];

    // Validate comments
    for (const [i, c] of reviewComments.entries()) {
      if (!c.path || !c.line || !c.body) {
        console.error(`Comment [${i}] missing required field (path, line, body).`);
        process.exit(1);
      }
      if (typeof c.line !== "number" || c.line < 1) {
        console.error(`Comment [${i}] line must be a positive integer. Got: ${c.line}`);
        process.exit(1);
      }
    }
  }
} else if (args.length === 3) {
  // Simple mode: <pr-number> <event> <body-file>
  prNumber = args[0];
  reviewEvent = args[1].toUpperCase();
  reviewBody = readFileSync(resolve(args[2]), "utf8");
  reviewComments = [];
} else {
  console.error("Usage:");
  console.error("  Review:     node squad-review.js <pr-number> <review-file.json>");
  console.error("  Reply:      node squad-review.js <pr-number> <reply-file.json>");
  console.error("  Threads:    node squad-review.js <pr-number> threads [--unresolved]");
  console.error("  Simple:     node squad-review.js <pr-number> <event> <body-file>");
  process.exit(1);
}

const validEvents = ["APPROVE", "REQUEST_CHANGES", "COMMENT"];
if (mode === "review" && !validEvents.includes(reviewEvent)) {
  console.error(`Invalid event: ${reviewEvent}. Must be one of: ${validEvents.join(", ")}`);
  process.exit(1);
}
if (mode === "reply" && reviewEvent && !validEvents.includes(reviewEvent)) {
  console.error(`Invalid event: ${reviewEvent}. Must be one of: ${validEvents.join(", ")}`);
  process.exit(1);
}

// --- JWT generation (RS256, no external deps) ---
function base64url(buffer) {
  return Buffer.from(buffer).toString("base64url");
}

function createJWT(appId, pemPath) {
  const privateKey = readFileSync(pemPath, "utf8");
  const now = Math.floor(Date.now() / 1000);

  const header = base64url(JSON.stringify({ alg: "RS256", typ: "JWT" }));
  const payload = base64url(JSON.stringify({
    iat: now - 30,        // issued 30s ago (clock skew)
    exp: now + 5 * 60,    // expires in 5 minutes
    iss: appId,
  }));

  const signable = `${header}.${payload}`;
  const sign = createSign("RSA-SHA256");
  sign.update(signable);
  sign.end();
  const signature = sign.sign(privateKey, "base64url");

  return `${signable}.${signature}`;
}

// --- GitHub API helpers ---
async function getInstallationToken(jwt, installationId) {
  const res = await fetch(
    `https://api.github.com/app/installations/${installationId}/access_tokens`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${jwt}`,
        Accept: "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
      },
    }
  );

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Failed to get installation token (${res.status}): ${text}`);
  }

  const data = await res.json();
  return data.token;
}

async function submitReview(token, owner, repo, pr, event, body, comments) {
  const payload = {
    event: event,
    body: body,
  };

  if (comments && comments.length > 0) {
    payload.comments = comments.map((c) => ({
      path: c.path,
      line: c.line,
      side: "RIGHT",
      body: c.body,
    }));
  }

  const res = await fetch(
    `https://api.github.com/repos/${owner}/${repo}/pulls/${pr}/reviews`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        Accept: "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
        "Content-Type": "application/json",
      },
      body: JSON.stringify(payload),
    }
  );

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Failed to submit review (${res.status}): ${text}`);
  }

  return await res.json();
}

async function replyToComment(token, owner, repo, pr, commentId, body) {
  const res = await fetch(
    `https://api.github.com/repos/${owner}/${repo}/pulls/${pr}/comments/${commentId}/replies`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        Accept: "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ body }),
    }
  );

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Failed to reply to comment ${commentId} (${res.status}): ${text}`);
  }

  return await res.json();
}

async function listReviewThreads(token, owner, repo, pr) {
  const query = `
    query($owner: String!, $repo: String!, $pr: Int!) {
      repository(owner: $owner, name: $repo) {
        pullRequest(number: $pr) {
          reviewThreads(first: 100) {
            nodes {
              id
              isResolved
              comments(first: 1) {
                nodes {
                  databaseId
                  path
                  originalLine
                  body
                  author { login }
                  createdAt
                }
              }
            }
          }
        }
      }
    }
  `;

  const res = await fetch("https://api.github.com/graphql", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: "application/vnd.github+json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      query,
      variables: { owner, repo, pr: parseInt(pr, 10) },
    }),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Failed to list review threads (${res.status}): ${text}`);
  }

  const data = await res.json();
  if (data.errors) {
    throw new Error(`GraphQL errors: ${JSON.stringify(data.errors)}`);
  }

  const threads = data.data.repository.pullRequest.reviewThreads.nodes;
  return threads.map((t) => {
    const c = t.comments.nodes[0];
    return {
      threadId: t.id,
      commentId: c.databaseId,
      path: c.path,
      line: c.originalLine,
      body: c.body,
      author: c.author?.login || "unknown",
      createdAt: c.createdAt,
      isResolved: t.isResolved,
    };
  });
}

async function resolveThread(token, threadId) {
  const query = `
    mutation($threadId: ID!) {
      resolveReviewThread(input: { threadId: $threadId }) {
        thread { isResolved }
      }
    }
  `;

  // Try via app token first
  const res = await fetch("https://api.github.com/graphql", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: "application/vnd.github+json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ query, variables: { threadId } }),
  });

  if (res.ok) {
    const data = await res.json();
    if (!data.errors) {
      return data.data.resolveReviewThread.thread.isResolved;
    }
    // FORBIDDEN — fall through to gh CLI fallback
  }

  // Fallback: use gh CLI (user's own auth can resolve threads)
  try {
    const ghQuery = `mutation { resolveReviewThread(input: {threadId: "${threadId}"}) { thread { isResolved } } }`;
    const tmpFile = resolve(tmpdir(), `squad-resolve-${Date.now()}.graphql`);
    writeFileSync(tmpFile, ghQuery, "utf8");
    const proc = spawnSync("gh", ["api", "graphql", "-F", `query=@${tmpFile}`], {
      encoding: "utf8",
      stdio: ["pipe", "pipe", "pipe"],
    });
    if (proc.status !== 0) {
      throw new Error(proc.stderr || "gh CLI failed");
    }
    const data = JSON.parse(proc.stdout);
    return data.data.resolveReviewThread.thread.isResolved;
  } catch (ghErr) {
    throw new Error(`Failed to resolve thread ${threadId} — both app token and gh CLI failed. ${ghErr.message}`);
  }
}

// --- Main ---
try {
  console.log(`Generating JWT for App ID ${APP_ID}...`);
  const jwt = createJWT(APP_ID, PEM_PATH);

  console.log(`Getting installation token for installation ${INSTALLATION_ID}...`);
  const token = await getInstallationToken(jwt, INSTALLATION_ID);

  if (mode === "threads") {
    // --- Threads mode: list review threads ---
    console.log(`Listing review threads on ${OWNER}/${REPO}#${prNumber}...`);
    let threads = await listReviewThreads(token, OWNER, REPO, prNumber);

    if (threadsUnresolvedOnly) {
      threads = threads.filter((t) => !t.isResolved);
    }

    // Output as JSON for consumption by the coordinator
    console.log(JSON.stringify(threads, null, 2));
    console.log(`\n${threads.length} thread(s) found.`);

  } else if (mode === "reply") {
    // --- Reply mode: respond to existing threads, optionally resolve ---
    const needsResolve = reviewReplies.some((r) => r.resolve);
    let threadMap = null;

    if (needsResolve) {
      // Build commentId → threadId mapping for resolving
      console.log(`Fetching thread metadata for resolve...`);
      const threads = await listReviewThreads(token, OWNER, REPO, prNumber);
      threadMap = new Map(threads.map((t) => [t.commentId, t.threadId]));
    }

    // Post replies
    let replyCount = 0;
    let resolveCount = 0;

    for (const reply of reviewReplies) {
      console.log(`Replying to comment ${reply.commentId}...`);
      await replyToComment(token, OWNER, REPO, prNumber, reply.commentId, reply.body);
      replyCount++;

      if (reply.resolve && threadMap) {
        const threadId = threadMap.get(reply.commentId);
        if (threadId) {
          console.log(`Resolving thread for comment ${reply.commentId}...`);
          await resolveThread(token, threadId);
          resolveCount++;
        } else {
          console.warn(`⚠️ Could not find thread for comment ${reply.commentId} — skipping resolve.`);
        }
      }
    }

    // Optionally submit a top-level review alongside the replies (with new comments if any)
    if (reviewEvent && reviewBody) {
      const newCommentCount = reviewComments.length;
      const newLabel = newCommentCount > 0 ? ` with ${newCommentCount} new inline comment(s)` : "";
      console.log(`Submitting ${reviewEvent} review on ${OWNER}/${REPO}#${prNumber}${newLabel}...`);
      const review = await submitReview(token, OWNER, REPO, prNumber, reviewEvent, reviewBody, reviewComments);
      console.log(`✅ Review posted: ${review.html_url}`);
    }

    const resolveLabel = resolveCount > 0 ? `, ${resolveCount} resolved` : "";
    const newCommentLabel = reviewComments.length > 0 ? `, ${reviewComments.length} new comment(s)` : "";
    console.log(`✅ ${replyCount} reply(s) posted${resolveLabel}${newCommentLabel}.`);

  } else {
    // --- Review mode: initial review with inline comments ---
    const commentCount = reviewComments.length;
    const commentLabel = commentCount > 0 ? ` with ${commentCount} inline comment(s)` : "";
    console.log(`Submitting ${reviewEvent} review on ${OWNER}/${REPO}#${prNumber}${commentLabel}...`);
    const review = await submitReview(token, OWNER, REPO, prNumber, reviewEvent, reviewBody, reviewComments);

    console.log(`✅ Review posted: ${review.html_url}`);
  }
} catch (err) {
  console.error(`❌ ${err.message}`);
  process.exit(1);
}
