const { spawn } = require("child_process");
const path = require("path");

const dllPath = path.resolve(__dirname, "temp/dev-mcp/bin/Precept.Mcp/debug/Precept.Mcp.dll");

const child = spawn("dotnet", [dllPath], {
  cwd: __dirname,
  stdio: ["pipe", "pipe", "pipe"]
});

let buffer = "";
let initialized = false;

function send(msg) {
  const json = JSON.stringify(msg);
  const header = `Content-Length: ${Buffer.byteLength(json)}\r\n\r\n`;
  process.stderr.write(`SENDING: ${json}\n`);
  child.stdin.write(header + json);
}

child.stdout.on("data", (chunk) => {
  process.stderr.write(`GOT DATA: ${chunk.length} bytes\n`);
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
    process.stderr.write(`MSG id=${msg.id} method=${msg.method}\n`);

    if (!initialized && msg.id === 1) {
      initialized = true;
      send({ jsonrpc: "2.0", method: "notifications/initialized", params: {} });
      send({ jsonrpc: "2.0", id: 2, method: "tools/list", params: {} });
    } else if (msg.id === 2) {
      process.stdout.write(JSON.stringify(msg, null, 2) + "\n");
      child.kill();
      process.exit(0);
    }
  }
});

child.stderr.on("data", (d) => process.stderr.write(`DOTNET STDERR: ${d}`));
child.on("exit", (code) => { process.stderr.write(`EXIT: ${code}\n`); process.exit(0); });

setTimeout(() => {
  process.stderr.write("TIMEOUT - sending init now\n");
  send({
    jsonrpc: "2.0",
    id: 1,
    method: "initialize",
    params: {
      protocolVersion: "2024-11-05",
      capabilities: {},
      clientInfo: { name: "test", version: "1.0" }
    }
  });
}, 2000);
