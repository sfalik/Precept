const { spawn } = require("child_process");
const path = require("path");
const dllPath = path.resolve(__dirname, "temp/dev-mcp/bin/Precept.Mcp/debug/Precept.Mcp.dll");

const child = spawn("dotnet", [dllPath], { cwd: __dirname, stdio: ["pipe", "pipe", "pipe"] });

function send(msg) { child.stdin.write(JSON.stringify(msg) + "\n"); }

let buffer = "";
let initialized = false;

child.stdout.on("data", (chunk) => {
  buffer += chunk.toString("utf8");
  const lines = buffer.split("\n");
  buffer = lines.pop();
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed || /^(info|warn|error|debug|crit|fail|trce):/.test(trimmed)) continue;
    let msg;
    try { msg = JSON.parse(trimmed); } catch { continue; }
    if (!initialized && msg.id === 1) {
      initialized = true;
      send({ jsonrpc: "2.0", method: "notifications/initialized", params: {} });
      send({ jsonrpc: "2.0", id: 2, method: "tools/list", params: {} });
    } else if (msg.id === 2) {
      process.stdout.write(JSON.stringify(msg, null, 2) + "\n");
      child.kill(); process.exit(0);
    }
  }
});

child.stderr.on("data", (d) => process.stderr.write(d.toString()));
child.on("exit", () => process.exit(0));

setTimeout(() => {
  send({ jsonrpc: "2.0", id: 1, method: "initialize",
    params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "test", version: "1.0" } } });
}, 2000);
setTimeout(() => { child.kill(); process.exit(1); }, 15000);
