const { spawn } = require('child_process');
const proc = spawn('node', ['tools/scripts/start-precept-mcp.js'], {
  cwd: 'C:\\Users\\Shane.Falik\\source\\repos\\precept-architecture',
  stdio: ['pipe', 'pipe', 'pipe']
});

let stdoutBuf = '';
proc.stdout.on('data', d => { stdoutBuf += d.toString(); });

function sendLine(obj) {
  proc.stdin.write(JSON.stringify(obj) + '\n');
}

sendLine({jsonrpc:'2.0',id:1,method:'initialize',params:{protocolVersion:'2024-11-05',capabilities:{},clientInfo:{name:'test',version:'1'}}});

setTimeout(() => {
  sendLine({jsonrpc:'2.0',method:'notifications/initialized',params:{}});
  sendLine({jsonrpc:'2.0',id:2,method:'tools/list',params:{}});
  setTimeout(() => {
    proc.kill();
    process.stdout.write('---BEGIN---\n' + stdoutBuf + '\n---END---\n');
  }, 5000);
}, 1000);
