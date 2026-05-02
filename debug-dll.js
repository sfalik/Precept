const { spawn } = require("child_process");
const path = require("path");
const fs = require("fs");

const workspaceRoot = "C:\\Users\\Shane.Falik\\source\\repos\\precept-architecture";
const dllPath = path.join(workspaceRoot, "temp", "dev-mcp", "bin", "Precept.Mcp", "debug", "Precept.Mcp.dll");

const child = spawn("dotnet", [dllPath], {
  cwd: workspaceRoot,
  stdio: ["pipe", "pipe", "pipe"]
});

let stderrData = "";
child.stdout.on("data", (d) => { process.stdout.write("STDOUT:" + d.toString() + "\n"); });
child.stderr.on("data", (d) => { stderrData += d.toString(); process.stderr.write("STDERR:" + d.toString()); });
child.on("exit", (code) => { process.stdout.write("EXIT:" + code + "\n"); });
child.on("error", (e) => { process.stdout.write("ERROR:" + e.message + "\n"); });

// Just send initialize and wait
function send(msg) {
  const json = JSON.stringify(msg);
  child.stdin.write(`Content-Length: ${Buffer.byteLength(json)}\r\n\r\n${json}`);
}

send({
  jsonrpc: "2.0", id: 1,
  method: "initialize",
  params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "debug", version: "1.0" } }
});

setTimeout(() => {
  process.stdout.write("DONE_WAITING\n");
  child.kill();
}, 5000);
