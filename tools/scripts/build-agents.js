// Builds agents and skills for both Claude Code and GitHub Copilot from a
// single platform-aware source tree.
//
//   tools/agent-sources/<slug>/{body.md, claude.yaml, copilot.yaml}
//     → .claude/agents/<slug>.md  + .github/agents/<slug>.agent.md
//
//   tools/skill-sources/<slug>/{body.md, claude.yaml, copilot.yaml}
//     → .claude/skills/<slug>/SKILL.md  + .github/skills/<slug>/SKILL.md
//
// Platform-specific content blocks in body.md are gated by:
//   <!-- ::if platform=claude -->...<!-- ::endif -->
//   <!-- ::if platform=copilot -->...<!-- ::endif -->

const fs = require("fs");
const path = require("path");

const workspaceRoot = path.resolve(__dirname, "..", "..");

const KINDS = {
  agent: {
    sourcesRoot: path.join(workspaceRoot, "tools", "agent-sources"),
    platforms: {
      copilot: {
        frontmatter: "copilot.yaml",
        output: (slug) => path.join(workspaceRoot, ".github", "agents", `${slug}.agent.md`),
      },
      claude: {
        frontmatter: "claude.yaml",
        output: (slug) => path.join(workspaceRoot, ".claude", "agents", `${slug}.md`),
      },
    },
  },
  skill: {
    sourcesRoot: path.join(workspaceRoot, "tools", "skill-sources"),
    platforms: {
      copilot: {
        frontmatter: "copilot.yaml",
        output: (slug) => path.join(workspaceRoot, ".github", "skills", slug, "SKILL.md"),
      },
      claude: {
        frontmatter: "claude.yaml",
        output: (slug) => path.join(workspaceRoot, ".claude", "skills", slug, "SKILL.md"),
      },
    },
  },
};

function filterPlatformBlocks(body, platform) {
  const blockRe = /<!--\s*::if\s+platform=(\w+)\s*-->([\s\S]*?)<!--\s*::endif\s*-->\n?/g;
  return body.replace(blockRe, (_match, blockPlatform, content) =>
    blockPlatform === platform ? content : ""
  );
}

function buildOne(kindConfig, slug, platform) {
  const sourceDir = path.join(kindConfig.sourcesRoot, slug);
  const platformConfig = kindConfig.platforms[platform];

  const frontmatter = fs.readFileSync(path.join(sourceDir, platformConfig.frontmatter), "utf8").trimEnd();
  const body = fs.readFileSync(path.join(sourceDir, "body.md"), "utf8");
  const rendered = filterPlatformBlocks(body, platform).replace(/\n{3,}/g, "\n\n");

  const output = `---\n${frontmatter}\n---\n\n${rendered.trimStart()}`;
  const outPath = platformConfig.output(slug);
  fs.mkdirSync(path.dirname(outPath), { recursive: true });
  fs.writeFileSync(outPath, output);
  process.stdout.write(`Built ${path.relative(workspaceRoot, outPath)}\n`);
}

function main() {
  for (const kindConfig of Object.values(KINDS)) {
    if (!fs.existsSync(kindConfig.sourcesRoot)) continue;

    const slugs = fs
      .readdirSync(kindConfig.sourcesRoot, { withFileTypes: true })
      .filter((entry) => entry.isDirectory())
      .map((entry) => entry.name);

    for (const slug of slugs) {
      for (const platform of Object.keys(kindConfig.platforms)) {
        buildOne(kindConfig, slug, platform);
      }
    }
  }
}

main();
