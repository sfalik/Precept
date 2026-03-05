# General ToDo list
- ~~extend samples with rules~~ — done; all 15 samples now demonstrate rules
- integration test for samples?
- implement field editing
- change name from statemachine to something else more generic?
- CLI (`smcli`) — design complete (see docs/CliDesign.md), implementation pending
  - runtime API extensions: `GetAvailableEvents`, `CoerceEventArguments`, `SerializeInstanceData`, `DeserializeInstanceData`
  - refactor `SmPreviewHandler` to use new runtime methods
  - scaffold `tools/StateMachine.Dsl.Cli/` project
  - implement REPL and one-shot modes