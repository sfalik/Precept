const { spawn } = require("child_process");
const path = require("path");
const fs = require("fs");
const dllPath = path.resolve(__dirname, "temp/dev-mcp/bin/Precept.Mcp/debug/Precept.Mcp.dll");
const dslText = fs.readFileSync(path.resolve(__dirname, "tools/scripts/_dsl-input.txt"), "utf8");

const child = spawn("dotnet", [dllPath], { cwd: __dirname, stdio: ["pipe", "pipe", "pipe"] });

// Use newline-delimited JSON (NDJSON) - NOT LSP Content-Length framing
function send(msg) {
  child.stdin.write(JSON.stringify(msg) + "\n");
}

let buffer = "";
let initialized = false;
let resultPrinted = false;

child.stdout.on("data", (chunk) => {
  buffer += chunk.toString("utf8");
  const lines = buffer.split("\n");
  buffer = lines.pop(); // keep incomplete last line
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    // Skip log messages (start with "info:" etc.)
    if (/^(info|warn|error|debug|crit|fail|trce):/.test(trimmed)) continue;
    let msg;
    try { msg = JSON.parse(trimmed); } catch { continue; }
    
    if (!initialized && msg.id === 1) {
      initialized = true;
      send({ jsonrpc: "2.0", method: "notifications/initialized", params: {} });
      send({ jsonrpc: "2.0", id: 2, method: "tools/call", params: { name: "precept_compile", arguments: { text: dslText } } });
    } else if (msg.id === 2) {
      process.stdout.write(JSON.stringify(msg, null, 2) + "\n");
      resultPrinted = true;
      child.kill();
      process.exit(0);
    }
  }
});

child.stderr.on("data", (d) => process.stderr.write(d.toString()));
child.on("exit", (code) => { if (!resultPrinted) { process.stderr.write(`EXIT without result: ${code}\n`); process.exit(1); } });

setTimeout(() => {
  send({
    jsonrpc: "2.0", id: 1, method: "initialize",
    params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "mcp-call", version: "1.0" } }
  });
}, 2000);

setTimeout(() => { if (!resultPrinted) { process.stderr.write("TIMEOUT\n"); child.kill(); process.exit(1); } }, 60000);
