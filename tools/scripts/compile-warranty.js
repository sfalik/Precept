const { spawn } = require("child_process");
const path = require("path");

const dllPath = path.resolve(__dirname, "../../temp/dev-mcp/bin/Precept.Mcp/release/Precept.Mcp.dll");
const toolName = "precept_compile";
const toolArgs = {
  text: `precept WarrantyRepairRequest

field CustomerName as string optional
field ProductName as string optional
field SerialNumber as string optional
field ApprovalNote as string optional
field LastReversedStep as string optional
field RepairComplete as boolean default false
field ShippingLabelSent as boolean default false
field RepairSteps as stack of string

state Draft initial
state Submitted
state Approved
state InRepair
state ReadyToReturn
state Closed
state Denied

in ReadyToReturn ensure RepairComplete because "Ready-to-return cases must have finished repair work"
in Closed ensure ShippingLabelSent because "Closed cases must have a return label on record"

to ReadyToReturn -> set ShippingLabelSent = true

event Submit(Customer as string notempty, Product as string notempty, Serial as string notempty)
event Approve(Note as string optional notempty)
event Deny(Note as string notempty)
event StartRepair
event LogRepairStep(StepName as string notempty)
event UndoLastStep
event FinishRepair
event ConfirmReturn

from Draft on Submit
    -> set CustomerName = Submit.Customer
    -> set ProductName = Submit.Product
    -> set SerialNumber = Submit.Serial
    -> transition Submitted

from Submitted on Approve
    -> set ApprovalNote = Approve.Note
    -> transition Approved
from Submitted on Deny
    -> set ApprovalNote = Deny.Note
    -> transition Denied

from Approved on StartRepair
    -> transition InRepair

from InRepair on LogRepairStep
    -> push RepairSteps LogRepairStep.StepName
    -> no transition
from InRepair on UndoLastStep when RepairSteps.count > 0
    -> pop RepairSteps into LastReversedStep
    -> no transition
from InRepair on UndoLastStep
    -> reject "There is no logged repair step to undo"
from InRepair on FinishRepair when RepairSteps.count > 0
    -> set RepairComplete = true
    -> transition ReadyToReturn
from InRepair on FinishRepair
    -> reject "At least one repair step must be logged before the repair can finish"

from ReadyToReturn on ConfirmReturn
    -> clear RepairSteps
    -> transition Closed`
};

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
      send({ jsonrpc: "2.0", id: 2, method: "tools/call", params: { name: toolName, arguments: toolArgs } });
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
    clientInfo: { name: "mcp-call", version: "1.0" }
  }
});
