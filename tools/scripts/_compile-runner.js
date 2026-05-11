// Temporary compile runner - reads DSL from _dsl-input.txt and calls precept_compile
const { spawn } = require("child_process");
const fs = require("fs");
const path = require("path");

const dllPath = path.resolve(__dirname, "../../temp/dev-mcp/bin/Precept.Mcp/release/Precept.Mcp.dll");
const dslText = fs.readFileSync(path.resolve(__dirname, "../../tools/scripts/_dsl-input.txt"), "utf8");
const toolArgs = { text: dslText };

const child = spawn("dotnet", [dllPath], {
  cwd: path.resolve(__dirname, "../.."),
  stdio: ["pipe", "pipe", "pipe"]
});

let buffer = "";
let initialized = false;
let resultPrinted = false;

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
      send({ jsonrpc: "2.0", id: 2, method: "tools/call", params: { name: "precept_compile", arguments: toolArgs } });
    } else if (msg.id === 2) {
      process.stdout.write(JSON.stringify(msg, null, 2) + "\n");
      resultPrinted = true;
      child.kill();
      process.exit(0);
    }
  }
});

child.stderr.on("data", (d) => process.stderr.write(d));
child.on("exit", () => { if (!resultPrinted) process.exit(1); });

send({
  jsonrpc: "2.0",
  id: 1,
  method: "initialize",
  params: {
    protocolVersion: "2024-11-05",
    capabilities: {},
    clientInfo: { name: "compile-runner", version: "1.0" }
  }
});
