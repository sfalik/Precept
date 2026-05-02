const { spawn } = require("child_process");
const path = require("path");
const dllPath = path.resolve(__dirname, "temp/dev-mcp/bin/Precept.Mcp/debug/Precept.Mcp.dll");

const child = spawn("dotnet", [dllPath], {
  cwd: __dirname,
  stdio: ["pipe", "pipe", "pipe"]
});

let buffer = "";
let gotSomething = false;

child.stdout.on("data", (chunk) => {
  buffer += chunk.toString();
  console.error("STDOUT DATA:", JSON.stringify(chunk.toString().slice(0, 200)));
  gotSomething = true;
});
child.stderr.on("data", (d) => {
  process.stderr.write("STDERR: " + d.toString());
});
child.on("exit", (code) => {
  console.error("EXITED:", code);
  process.exit(0);
});

function send(msg) {
  const json = JSON.stringify(msg);
  const frame = `Content-Length: ${Buffer.byteLength(json)}\r\n\r\n${json}`;
  console.error("SENDING:", JSON.stringify(frame.slice(0, 100)));
  child.stdin.write(frame);
}

setTimeout(() => {
  console.error("Sending initialize...");
  send({
    jsonrpc: "2.0", id: 1, method: "initialize",
    params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "test", version: "1.0" } }
  });
}, 500);

setTimeout(() => {
  if (!gotSomething) console.error("NO OUTPUT after 15s");
  child.kill();
  process.exit(0);
}, 15000);
