const { spawn } = require("child_process");
const path = require("path");

const workspaceRoot = "C:\\Users\\Shane.Falik\\source\\repos\\precept-architecture";
const dllPath = path.join(workspaceRoot, "temp", "dev-mcp", "bin", "Precept.Mcp", "debug", "Precept.Mcp.dll");

const text = `precept SubscriptionCancellationRetention

field SubscriberName as string optional
field PlanName as string optional
field MonthlyPrice as number default 0 nonnegative
field SaveOfferAccepted as boolean default false
field RetentionDiscount as number default 0 nonnegative max 100
field CancellationReason as string optional
field LastAgentNote as string optional

state Active initial
state RetentionReview
state Cancelled

in RetentionReview modify LastAgentNote editable

event RequestCancellation(
    Name as string notempty,
    Plan as string notempty,
    Price as number,
    Reason as string notempty)
on RequestCancellation ensure RequestCancellation.Price >= 0 because "The plan price cannot be negative"

event MakeSaveOffer(DiscountPercent as number default 10)
on MakeSaveOffer ensure MakeSaveOffer.DiscountPercent > 0 because "Save offers must include a positive discount"
on MakeSaveOffer ensure MakeSaveOffer.DiscountPercent <= 100 because "Save offers cannot exceed 100 percent"

event AcceptOffer
event DeclineOffer(Note as string optional notempty)

from Active on RequestCancellation
    -> set SubscriberName = RequestCancellation.Name
    -> set PlanName = RequestCancellation.Plan
    -> set MonthlyPrice = RequestCancellation.Price
    -> set CancellationReason = RequestCancellation.Reason
    -> set SaveOfferAccepted = false
    -> transition RetentionReview

from RetentionReview on MakeSaveOffer when not SaveOfferAccepted and RetentionDiscount == 0
    -> set RetentionDiscount = MakeSaveOffer.DiscountPercent
    -> set LastAgentNote = if MonthlyPrice >= 50 then "Premium plan retention" else "Standard plan retention"
    -> no transition
from RetentionReview on MakeSaveOffer
    -> reject "Only one outstanding save offer can exist at a time"

from RetentionReview on AcceptOffer
    -> set SaveOfferAccepted = true
    -> clear CancellationReason
    -> transition Active
from RetentionReview on DeclineOffer
    -> set LastAgentNote = DeclineOffer.Note
    -> transition Cancelled`;

const child = spawn("dotnet", [dllPath], {
  cwd: workspaceRoot,
  stdio: ["pipe", "pipe", "pipe"]
});

let stdout = "";
let initialized = false;
let listDone = false;
let compileDone = false;

function send(msg) {
  child.stdin.write(JSON.stringify(msg) + "\n");
}

child.stdout.on("data", (data) => {
  stdout += data.toString();
  const lines = stdout.split("\n");
  stdout = lines.pop();
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    let msg;
    try { msg = JSON.parse(trimmed); } catch { continue; }

    if (!initialized && msg.id === 1) {
      initialized = true;
      send({ jsonrpc: "2.0", method: "notifications/initialized", params: {} });
      send({ jsonrpc: "2.0", id: 2, method: "tools/list", params: {} });
    } else if (msg.id === 2 && !listDone) {
      listDone = true;
      process.stdout.write("TOOLS_LIST: " + JSON.stringify(msg, null, 2) + "\n");
      send({ jsonrpc: "2.0", id: 3, method: "tools/call", params: { name: "precept_compile", arguments: { text } } });
    } else if (msg.id === 3) {
      compileDone = true;
      process.stdout.write("COMPILE_RESULT: " + JSON.stringify(msg, null, 2) + "\n");
      child.kill();
      process.exit(0);
    }
  }
});

child.stderr.on("data", (d) => process.stderr.write(d));
child.on("exit", () => { if (!compileDone) process.exit(1); });

send({
  jsonrpc: "2.0", id: 1,
  method: "initialize",
  params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "compile-temp", version: "1.0" } }
});

setTimeout(() => {
  if (!compileDone) {
    process.stderr.write("TIMEOUT\n");
    child.kill();
    process.exit(1);
  }
}, 30000);
