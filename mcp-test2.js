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
let resultPrinted = false;

child.stdout.on("data", (chunk) => {
  stdoutBuf += chunk.toString();
  process.stderr.write("STDOUT: " + JSON.stringify(chunk.toString().slice(0,300)) + "\n");
  // Try to parse lines as JSON
  const lines = stdoutBuf.split("\n");
  stdoutBuf = lines.pop() || "";
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    process.stderr.write("LINE: " + JSON.stringify(trimmed.slice(0,100)) + "\n");
    try {
      const msg = JSON.parse(trimmed);
      process.stderr.write("PARSED MSG id=" + msg.id + "\n");
      if (!initialized && msg.id === 1) {
        initialized = true;
        // Send initialized notification then tools/call
        send({ jsonrpc: "2.0", method: "notifications/initialized", params: {} });
        const dslText = "precept TestPrecept\nfield MyField as string optional\nstate Open initial\nstate Closed";
        send({ jsonrpc: "2.0", id: 2, method: "tools/call", params: { name: "precept_compile", arguments: { text: dslText } } });
      } else if (msg.id === 2) {
        process.stdout.write(JSON.stringify(msg, null, 2) + "\n");
        resultPrinted = true;
        child.kill();
        process.exit(0);
      }
    } catch(e) {}
  }
});
child.stderr.on("data", (d) => process.stderr.write("STDERR: " + d));
child.on("exit", (code) => { process.stderr.write("EXIT: " + code + "\n"); process.exit(0); });

function send(msg) {
  const json = JSON.stringify(msg) + "\n";
  child.stdin.write(json);
}

setTimeout(() => {
  process.stderr.write("Sending initialize...\n");
  send({
    jsonrpc: "2.0", id: 1, method: "initialize",
    params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "test", version: "1.0" } }
  });
}, 500);

setTimeout(() => {
  if (!resultPrinted) { process.stderr.write("TIMEOUT\n"); child.kill(); process.exit(1); }
}, 20000);
