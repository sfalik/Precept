const fs = require("fs");
const path = require("path");

const workspaceRoot = path.resolve(__dirname, "..", "..");
const settingsPath = path.join(workspaceRoot, ".vscode", "settings.json");
const pluginKey = "./tools/Precept.Plugin";

const flag = process.argv[2];
if (flag !== "--enable" && flag !== "--disable") {
  console.error("Usage: toggle-plugin.js --enable | --disable");
  process.exit(1);
}

const enable = flag === "--enable";

let settings = {};
if (fs.existsSync(settingsPath)) {
  const raw = fs.readFileSync(settingsPath, "utf8");
  settings = JSON.parse(raw);
}

const locations = settings["chat.pluginLocations"] ?? {};
locations[pluginKey] = enable;
settings["chat.pluginLocations"] = locations;

fs.writeFileSync(settingsPath, JSON.stringify(settings, null, 2) + "\n", "utf8");
console.log(`Plugin ${enable ? "enabled" : "disabled"}. Reload window to apply.`);
