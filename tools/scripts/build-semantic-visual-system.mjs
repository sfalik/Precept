import { readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptPath = fileURLToPath(import.meta.url);
const scriptDir = path.dirname(scriptPath);
const repoRoot = path.resolve(scriptDir, "..", "..");
const sourceRoot = path.join(repoRoot, "design", "system", "foundations", "source");
const templatePath = path.join(sourceRoot, "shell", "template.html");
const navPath = path.join(sourceRoot, "data", "navigation.json");

const escapeHtml = (value) => value
  .replaceAll("&", "&amp;")
  .replaceAll("<", "&lt;")
  .replaceAll(">", "&gt;")
  .replaceAll('"', "&quot;");

const escapeAttribute = (value) => escapeHtml(value).replaceAll("'", "&#39;");

const toPosix = (value) => value.split(path.sep).join("/");

function renderInline(markdown) {
  const tokens = [];
  let rendered = escapeHtml(markdown);

  rendered = rendered.replace(/`([^`]+)`/g, (_, content) => {
    const token = `__CODE_${tokens.length}__`;
    tokens.push(`<code>${escapeHtml(content)}</code>`);
    return token;
  });

  rendered = rendered.replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>");
  rendered = rendered.replace(/\[([^\]]+)\]\(([^)]+)\)/g, (_, label, href) => {
    return `<a href="${escapeAttribute(href)}">${label}</a>`;
  });

  tokens.forEach((tokenValue, index) => {
    rendered = rendered.replace(`__CODE_${index}__`, tokenValue);
  });

  return rendered;
}

async function loadIsland(name) {
  const islandPath = path.join(sourceRoot, "islands", `${name}.html`);
  return readFile(islandPath, "utf8");
}

async function renderMarkdown(markdown) {
  const lines = markdown.replace(/\r\n/g, "\n").split("\n");
  const html = [];
  let islandCount = 0;

  for (let index = 0; index < lines.length; index += 1) {
    const rawLine = lines[index];
    const line = rawLine.trim();

    if (!line) {
      continue;
    }

    const islandMatch = line.match(/^<!--\s*island:\s*([a-z0-9-_/]+)\s*-->$/i);
    if (islandMatch) {
      islandCount += 1;
      html.push(await loadIsland(islandMatch[1]));
      continue;
    }

    const fenceMatch = rawLine.match(/^```([a-z0-9_-]+)?\s*$/i);
    if (fenceMatch) {
      const language = fenceMatch[1] ? ` language-${fenceMatch[1]}` : "";
      const buffer = [];
      index += 1;
      while (index < lines.length && !/^```\s*$/.test(lines[index])) {
        buffer.push(lines[index]);
        index += 1;
      }
      html.push(`<pre class="code-block"><code class="${language.trim()}">${escapeHtml(buffer.join("\n"))}</code></pre>`);
      continue;
    }

    const headingMatch = rawLine.match(/^(#{1,6})\s+(.+)$/);
    if (headingMatch) {
      const level = Math.min(headingMatch[1].length + 1, 6);
      html.push(`<h${level}>${renderInline(headingMatch[2].trim())}</h${level}>`);
      continue;
    }

    const unorderedMatch = rawLine.match(/^[-*]\s+(.+)$/);
    if (unorderedMatch) {
      const items = [unorderedMatch[1]];
      while (index + 1 < lines.length) {
        const nextLine = lines[index + 1].trim();
        const nextMatch = nextLine.match(/^[-*]\s+(.+)$/);
        if (!nextMatch) {
          break;
        }
        items.push(nextMatch[1]);
        index += 1;
      }
      html.push(`<ul>${items.map((item) => `<li>${renderInline(item)}</li>`).join("")}</ul>`);
      continue;
    }

    const orderedMatch = rawLine.match(/^\d+\.\s+(.+)$/);
    if (orderedMatch) {
      const items = [orderedMatch[1]];
      while (index + 1 < lines.length) {
        const nextLine = lines[index + 1].trim();
        const nextMatch = nextLine.match(/^\d+\.\s+(.+)$/);
        if (!nextMatch) {
          break;
        }
        items.push(nextMatch[1]);
        index += 1;
      }
      html.push(`<ol>${items.map((item) => `<li>${renderInline(item)}</li>`).join("")}</ol>`);
      continue;
    }

    const paragraph = [rawLine.trim()];
    while (index + 1 < lines.length) {
      const nextRaw = lines[index + 1];
      const nextTrimmed = nextRaw.trim();
      if (!nextTrimmed) {
        break;
      }
      if (/^<!--\s*island:/.test(nextTrimmed) || /^```/.test(nextRaw) || /^(#{1,6})\s+/.test(nextRaw) || /^[-*]\s+/.test(nextRaw) || /^\d+\.\s+/.test(nextRaw)) {
        break;
      }
      paragraph.push(nextTrimmed);
      index += 1;
    }
    html.push(`<p>${renderInline(paragraph.join(" "))}</p>`);
  }

  return {
    html: html.join("\n"),
    islandCount,
  };
}

async function main() {
  const nav = JSON.parse(await readFile(navPath, "utf8"));
  const template = await readFile(templatePath, "utf8");
  const outputPath = path.join(repoRoot, "design", "system", "foundations", nav.output);

  let totalIslands = 0;
  const sections = [];

  for (const section of nav.sections) {
    const sectionPath = path.join(sourceRoot, section.file);
    const markdown = await readFile(sectionPath, "utf8");
    const rendered = await renderMarkdown(markdown);
    totalIslands += rendered.islandCount;

    sections.push(`
<section class="band band--${escapeAttribute(section.layout)}" id="${escapeAttribute(section.id)}" data-section data-title="${escapeAttribute(section.title)}" data-mode="${escapeAttribute(`${section.tier} / ${section.mode}`)}">
  <div class="band-shell">
    <header class="band-head">
      <div class="band-kicker">${escapeHtml(section.tier)} / ${escapeHtml(section.mode)}</div>
      <h2 class="band-title">${escapeHtml(section.title)}</h2>
      <p class="band-summary">${escapeHtml(section.summary)}</p>
    </header>
    <div class="band-body markdown-flow">
${rendered.html}
    </div>
  </div>
</section>`.trim());
  }

  const navItems = nav.sections.map((section) => {
    return `        <a class="section-rail-link" href="#${escapeAttribute(section.id)}" data-nav-link data-target="${escapeAttribute(section.id)}">${escapeHtml(section.nav)}</a>`;
  }).join("\n");

  const cssPath = toPosix(path.relative(path.dirname(outputPath), path.join(sourceRoot, "assets", "main.css")));
  const jsPath = toPosix(path.relative(path.dirname(outputPath), path.join(sourceRoot, "assets", "interactive.js")));

  const replacements = new Map([
    ["{{PAGE_TITLE}}", escapeHtml(nav.title)],
    ["{{PROTOTYPE_LABEL}}", escapeHtml(nav.prototypeLabel)],
    ["{{PAGE_DESCRIPTION}}", escapeAttribute(nav.description)],
    ["{{NAV_ITEMS}}", navItems],
    ["{{SECTIONS}}", sections.join("\n\n")],
    ["{{SECTION_COUNT}}", String(nav.sections.length)],
    ["{{ISLAND_COUNT}}", String(totalIslands)],
    ["{{FIRST_SECTION_TITLE}}", escapeHtml(nav.sections[0].title)],
    ["{{FIRST_SECTION_MODE}}", escapeHtml(`${nav.sections[0].tier} / ${nav.sections[0].mode}`)],
    ["{{BUILD_DATE}}", escapeHtml(new Date().toISOString())],
    ["{{ASSET_CSS}}", escapeAttribute(cssPath)],
    ["{{ASSET_JS}}", escapeAttribute(jsPath)],
  ]);

  let output = template;
  for (const [placeholder, value] of replacements.entries()) {
    output = output.replaceAll(placeholder, value);
  }

  const unresolved = output.match(/{{[A-Z_]+}}/g);
  if (unresolved) {
    throw new Error(`Unresolved template placeholders: ${unresolved.join(", ")}`);
  }

  await writeFile(outputPath, output, "utf8");
  console.log(`Built ${path.relative(repoRoot, outputPath)} with ${nav.sections.length} sections and ${totalIslands} islands.`);
}

main().catch((error) => {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
});