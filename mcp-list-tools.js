const { spawn } = require("child_process");
const path = require("path");
const dllPath = path.resolve(__dirname, "temp/dev-mcp/bin/Precept.Mcp/debug/Precept.Mcp.dll");

const child = spawn("dotnet", [dllPath], {
  cwd: __dirname,
  stdio: ["pipe", "pipe", "pipe"],
  env: { ...process.env, "Logging__LogLevel__Default": "None" }
});

let stdoutBuf = "";
let initialized = false;

child.stdout.on("data", (chunk) => {
  stdoutBuf += chunk.toString();
  const lines = stdoutBuf.split("\n");
  stdoutBuf = lines.pop() || "";
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    try {
      const msg = JSON.parse(trimmed);
      if (!initialized && msg.id === 1) {
        initialized = true;
        send({ jsonrpc: "2.0", method: "notifications/initialized", params: {} });
        // List tools
        send({ jsonrpc: "2.0", id: 2, method: "tools/list", params: {} });
      } else if (msg.id === 2) {
        process.stdout.write(JSON.stringify(msg, null, 2) + "\n");
        child.kill();
        process.exit(0);
      }
    } catch(e) {}
  }
});
child.stderr.on("data", () => {});
child.on("exit", () => process.exit(0));

function send(msg) { child.stdin.write(JSON.stringify(msg) + "\n"); }

setTimeout(() => send({
  jsonrpc: "2.0", id: 1, method: "initialize",
  params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "test", version: "1.0" } }
}), 300);
setTimeout(() => { child.kill(); process.exit(1); }, 15000);
