using StateMachine.Dsl;
var text = System.IO.File.ReadAllText("../../../../samples/trafficlight.sm");
try
{
    var machine = StateMachineDslParser.Parse(text);
    var def = DslWorkflowCompiler.Compile(machine);
    var instance = def.CreateInstance(def.InitialState);
    Console.WriteLine($"OK name={def.Name} initial={def.InitialState} current={instance.CurrentState} states={def.States.Count}");
}
catch (Exception ex)
{
    Console.WriteLine("ERR " + ex.Message);
    Environment.Exit(1);
}
