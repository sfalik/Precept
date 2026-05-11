// One-shot MCP client: initializes the server, calls one tool, prints result, exits.
const { spawn } = require("child_process");
const path = require("path");
const fs = require("fs");

const workspaceRoot = path.resolve(__dirname, "..", "..");
const buildBinRoot = path.join(workspaceRoot, "temp", "dev-mcp", "bin");
const mcpDllName = "Precept.Mcp.dll";

const toolName = process.argv[2];
const argsJson = process.argv[3];

if (!toolName || !argsJson) {
  process.stderr.write("Usage: node mcp_call.js <toolName> <argsJson>\n");
  process.exit(1);
}

const toolArgs = JSON.parse(argsJson);

function findDll() {
  const stack = [buildBinRoot];
  while (stack.length > 0) {
    const cur = stack.pop();
    if (!fs.existsSync(cur)) continue;
    for (const entry of fs.readdirSync(cur, { withFileTypes: true })) {
      const p = path.join(cur, entry.name);
      if (entry.isDirectory()) { stack.push(p); continue; }
      if (entry.name === mcpDllName) {
        const norm = p.replace(/\\/g, "/");
        if (norm.includes("/Precept.Mcp/") && (norm.includes("/debug/") || norm.includes("/release/"))) return p;
      }
    }
  }
  return null;
}

const dll = findDll();
if (!dll) {
  process.stderr.write("Could not find Precept.Mcp.dll\n");
  process.exit(1);
}

const child = spawn("dotnet", [dll], {
  cwd: workspaceRoot,
  stdio: ["pipe", "pipe", "pipe"]
});

child.stderr.on("data", (d) => process.stderr.write("STDERR: " + d));

let rawBuf = Buffer.alloc(0);

function send(obj) {
  const body = Buffer.from(JSON.stringify(obj), "utf8");
  const header = Buffer.from(`Content-Length: ${body.length}\r\n\r\n`, "utf8");
  child.stdin.write(Buffer.concat([header, body]));
}

function handleMessage(msg) {
  process.stderr.write("GOT id=" + msg.id + " method=" + (msg.method || "") + "\n");
  if (msg.id === 1) {
    send({ jsonrpc: "2.0", method: "notifications/initialized" });
    send({ jsonrpc: "2.0", id: 2, method: "tools/call", params: { name: toolName, arguments: toolArgs } });
  } else if (msg.id === 2) {
    process.stdout.write(JSON.stringify(msg, null, 2) + "\n");
    child.stdin.end();
    setTimeout(() => process.exit(0), 300);
  }
}

child.stdout.on("data", (chunk) => {
  rawBuf = Buffer.concat([rawBuf, chunk]);
  while (true) {
    const headerEnd = rawBuf.indexOf("\r\n\r\n");
    if (headerEnd === -1) break;
    const header = rawBuf.slice(0, headerEnd).toString("utf8");
    const clMatch = header.match(/Content-Length:\s*(\d+)/i);
    if (!clMatch) { rawBuf = rawBuf.slice(headerEnd + 4); continue; }
    const len = parseInt(clMatch[1], 10);
    const bodyStart = headerEnd + 4;
    if (rawBuf.length < bodyStart + len) break;
    const body = rawBuf.slice(bodyStart, bodyStart + len).toString("utf8");
    rawBuf = rawBuf.slice(bodyStart + len);
    let msg;
    try { msg = JSON.parse(body); } catch (e) { process.stderr.write("Parse error: " + e + "\n"); continue; }
    handleMessage(msg);
  }
});

child.on("exit", (code) => { process.stderr.write("Child exited: " + code + "\n"); process.exit(code ?? 0); });

send({
  jsonrpc: "2.0", id: 1, method: "initialize",
  params: {
    protocolVersion: "2024-11-05",
    capabilities: {},
    clientInfo: { name: "mcp_call", version: "1.0" }
  }
});
