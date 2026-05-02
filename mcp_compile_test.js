const { spawn } = require('child_process');

const dsl = `precept RestaurantWaitlist

field CurrentParty as string optional
field LastCalledParty as string optional
field EstimatedWaitMinutes as number default 0 nonnegative
field WalkInOpen as boolean default true
field PartyQueue as queue of string

state Accepting initial
state Seating
state Closed

in Seating ensure CurrentParty is set because "The seating state must know which party is being seated"

event JoinWaitlist(PartyName as string notempty)
event SeatNextParty
event MarkSeated
event CloseService
event ReopenService

from Accepting on JoinWaitlist
    -> enqueue PartyQueue JoinWaitlist.PartyName
    -> set EstimatedWaitMinutes = PartyQueue.count * 10
    -> no transition

from Accepting on SeatNextParty when PartyQueue.count > 0
    -> set LastCalledParty = PartyQueue.peek
    -> dequeue PartyQueue into CurrentParty
    -> transition Seating
from Accepting on SeatNextParty
    -> reject "No party is currently waiting"

from Seating on MarkSeated
    -> clear CurrentParty
    -> set EstimatedWaitMinutes = PartyQueue.count * 10
    -> transition Accepting

from any on CloseService
    -> set WalkInOpen = false
    -> transition Closed
from Closed on ReopenService
    -> set WalkInOpen = true
    -> set EstimatedWaitMinutes = PartyQueue.count * 10
    -> transition Accepting`;

const dllPath = 'C:\\Users\\Shane.Falik\\source\\repos\\precept-architecture\\temp\\dev-mcp\\bin\\Precept.Mcp\\debug\\Precept.Mcp.dll';

const proc = spawn('dotnet', [dllPath], {
  cwd: 'C:\\Users\\Shane.Falik\\source\\repos\\precept-architecture',
  stdio: ['pipe', 'pipe', 'pipe']
});

let buffer = '';

// Server uses newline-delimited JSON (not Content-Length framing)
function send(msg) {
  proc.stdin.write(JSON.stringify(msg) + '\n', 'utf8');
}

// Stderr: log messages from .NET hosting; ignore
proc.stderr.on('data', () => {});

proc.stdout.on('data', (data) => {
  buffer += data.toString('utf8');
  const lines = buffer.split('\n');
  buffer = lines.pop(); // keep incomplete last line
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    let msg;
    try { msg = JSON.parse(trimmed); } catch(e) { continue; } // skip non-JSON log lines

    if (msg.id === 1 && msg.result) {
      send({ jsonrpc: '2.0', method: 'notifications/initialized', params: {} });
      send({ jsonrpc: '2.0', id: 2, method: 'tools/call', params: {
        name: 'precept_compile',
        arguments: { text: dsl }
      }});
    } else if (msg.id === 2) {
      console.log(JSON.stringify(msg, null, 2));
      proc.stdin.end();
      setTimeout(() => { proc.kill(); process.exit(0); }, 500);
    }
  }
});

send({ jsonrpc: '2.0', id: 1, method: 'initialize', params: {
  protocolVersion: '2024-11-05',
  capabilities: {},
  clientInfo: { name: 'cli-test', version: '1.0' }
}});

setTimeout(() => {
  console.error('TIMEOUT. Buffer so far: ' + JSON.stringify(buffer.slice(0, 1000)));
  proc.kill();
  process.exit(1);
}, 30000);
