const { spawn } = require("child_process");
const path = require("path");
const dllPath = path.resolve(__dirname, "temp/dev-mcp/bin/Precept.Mcp/debug/Precept.Mcp.dll");

const child = spawn("dotnet", [dllPath], { cwd: __dirname, stdio: ["pipe", "pipe", "pipe"] });

function send(msg) {
  const json = JSON.stringify(msg);
  child.stdin.write(`Content-Length: ${Buffer.byteLength(json)}\r\n\r\n${json}`);
}

child.stdout.on("data", (chunk) => {
  // Dump raw hex of first 200 bytes for inspection
  const str = chunk.toString("utf8");
  process.stderr.write(`=== CHUNK (${chunk.length} bytes) ===\n`);
  process.stderr.write(str.substring(0, 300) + "\n");
  process.stderr.write(`===\n`);
});

child.stderr.on("data", (d) => process.stderr.write(`DOTNET: ${d}`));
child.on("exit", (code) => { process.stderr.write(`EXIT: ${code}\n`); });

setTimeout(() => {
  send({
    jsonrpc: "2.0", id: 1, method: "initialize",
    params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "test", version: "1.0" } }
  });
}, 2000);

setTimeout(() => { child.kill(); process.exit(0); }, 8000);
