const { spawn } = require("child_process");
const path = require("path");

const workspaceRoot = "C:\\Users\\Shane.Falik\\source\\repos\\precept-architecture";
const dllPath = path.join(workspaceRoot, "temp", "dev-mcp", "bin", "Precept.Mcp", "debug", "Precept.Mcp.dll");

const child = spawn("dotnet", [dllPath], {
  cwd: workspaceRoot,
  stdio: ["pipe", "pipe", "pipe"]
});

let buffer = "";
let initialized = false;
let done = false;

function send(msg) {
  const json = JSON.stringify(msg);
  child.stdin.write(`Content-Length: ${Buffer.byteLength(json)}\r\n\r\n${json}`);
}

child.stdout.on("data", (chunk) => {
  buffer += chunk.toString();
  while (true) {
    const headerEnd = buffer.indexOf("\r\n\r\n");
    if (headerEnd === -1) break;
    const headerSection = buffer.slice(0, headerEnd);
    const lengthMatch = headerSection.match(/Content-Length:\s*(\d+)/i);
    if (!lengthMatch) { buffer = buffer.slice(headerEnd + 4); break; }
    const length = parseInt(lengthMatch[1], 10);
    const bodyStart = headerEnd + 4;
    if (buffer.length < bodyStart + length) break;
    const body = buffer.slice(bodyStart, bodyStart + length);
    buffer = buffer.slice(bodyStart + length);
    let msg;
    try { msg = JSON.parse(body); } catch { continue; }

    if (!initialized && msg.id === 1) {
      initialized = true;
      send({ jsonrpc: "2.0", method: "notifications/initialized", params: {} });
      send({ jsonrpc: "2.0", id: 2, method: "tools/list", params: {} });
    } else if (msg.id === 2) {
      done = true;
      process.stdout.write(JSON.stringify(msg, null, 2) + "\n");
      child.kill();
      process.exit(0);
    }
  }
});

child.stderr.on("data", (d) => process.stderr.write(d));
child.on("exit", () => { if (!done) process.exit(1); });

send({
  jsonrpc: "2.0", id: 1,
  method: "initialize",
  params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "list-tools", version: "1.0" } }
});

setTimeout(() => {
  if (!done) { process.stderr.write("TIMEOUT\n"); child.kill(); process.exit(1); }
}, 15000);
