// One-shot compile of HiringPipeline DSL via MCP
const { spawn } = require("child_process");
const path = require("path");

const dllPath = path.resolve(__dirname, "../../temp/dev-mcp/bin/Precept.Mcp/debug/Precept.Mcp.dll");

const dsl = `precept HiringPipeline

field CandidateName as string optional maxlength 100
field RoleName as string optional
field RecruiterName as string optional
field FeedbackCount as integer default 0 nonnegative
field OfferAmount as number default 0 nonnegative
field FinalNote as string optional maxlength 500
field PendingInterviewers as set of string

state Draft initial
state Screening
state InterviewLoop
state Decision
state OfferExtended
state Hired
state Rejected

in InterviewLoop ensure PendingInterviewers.count > 0 because "Interview loops require at least one pending interviewer"
in Hired ensure OfferAmount > 0 because "Hired candidates must have an offer amount"

event SubmitApplication(
    Candidate as string notempty,
    Role as string notempty,
    Recruiter as string notempty)
event AddInterviewer(Name as string notempty)
event PassScreen
event RecordInterviewFeedback(Interviewer as string notempty)
event ExtendOffer(Amount as number positive)
event AcceptOffer
event RejectCandidate(Note as string notempty)

from Draft on SubmitApplication
    -> set CandidateName = SubmitApplication.Candidate
    -> set RoleName = SubmitApplication.Role
    -> set RecruiterName = SubmitApplication.Recruiter
    -> transition Screening

from Screening on AddInterviewer
    -> add PendingInterviewers AddInterviewer.Name
    -> no transition
from Screening on PassScreen when PendingInterviewers.count > 0
    -> transition InterviewLoop
from Screening on PassScreen
    -> reject "At least one interviewer must be assigned before the interview loop begins"
from Screening on RejectCandidate
    -> set FinalNote = RejectCandidate.Note
    -> transition Rejected

from InterviewLoop on RecordInterviewFeedback when PendingInterviewers contains RecordInterviewFeedback.Interviewer and PendingInterviewers.count == 1
    -> remove PendingInterviewers RecordInterviewFeedback.Interviewer
    -> set FeedbackCount = FeedbackCount + 1
    -> transition Decision
from InterviewLoop on RecordInterviewFeedback when PendingInterviewers contains RecordInterviewFeedback.Interviewer
    -> remove PendingInterviewers RecordInterviewFeedback.Interviewer
    -> set FeedbackCount = FeedbackCount + 1
    -> no transition
from InterviewLoop on RecordInterviewFeedback
    -> reject "Only assigned interviewers can submit feedback"
from InterviewLoop on RejectCandidate
    -> set FinalNote = RejectCandidate.Note
    -> transition Rejected

from Decision on ExtendOffer when FeedbackCount >= 2
    -> set OfferAmount = ExtendOffer.Amount
    -> set FinalNote = if FeedbackCount >= 3 then "Strong Hire" else "Standard Hire"
    -> transition OfferExtended
from Decision on ExtendOffer
    -> reject "At least two completed interview feedback entries are required before extending an offer"
from Decision on RejectCandidate
    -> set FinalNote = RejectCandidate.Note
    -> transition Rejected

from OfferExtended on AcceptOffer
    -> transition Hired`;

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
      send({ jsonrpc: "2.0", id: 2, method: "tools/call", params: { name: "precept_compile", arguments: { text: dsl } } });
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
    clientInfo: { name: "compile-hiring-pipeline", version: "1.0" }
  }
});

setTimeout(() => { console.error("Timeout"); child.kill(); process.exit(1); }, 60000);
