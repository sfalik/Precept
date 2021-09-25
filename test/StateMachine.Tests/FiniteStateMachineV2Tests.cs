using System;
using System.Threading.Tasks;
using Xunit;

namespace StateMachine.Tests
{

    enum Status
    {
        New, Planned, Approved, WorkStarted, Completed, Closed
    }

    delegate Task Approve(int a, int b);

    public class FiniteStateMachineV2Tests
    {
        [Fact]
        public async void Test1()
        {

            var workflow = StateMachine.CreateBuilder<Status>()
                .DefineEvent(out var approve, "Approve")
                    .WhenStateIs(Status.New)
                    .If(() => true, "because")
                    .TransitionTo(Status.Approved)

                .DefineAsyncTrigger<string>(out var close)
                    .WhenStateIs(Status.Completed)
                    .Execute(async reason => await Task.CompletedTask)
                    .ThenTransitionTo(Status.Closed)

                .DefineTrigger<DateTime>(out var startWork)
                    .WhenStateIs(Status.Planned, Status.Approved)
                        .If(time => time < DateTime.Now, "Because work cannot start in the past")
                            .Execute(time => { })
                            .ThenTransitionTo(Status.WorkStarted)
                        .Else.If(time => false, "because")
                            .Execute(time => { })
                            .AndKeepSameState()
                    .WhenStateIs(Status.Closed)
                        .TransitionTo(Status.Approved)



                .Build(Status.New)
            ;

            if (approve.IsAccepted)
                approve.Trigger();

            await close("because");

            var result = workflow.TestTrigger(close, "Test");
            if (result.IsAccepted)
            {

            }

            if (workflow.TestTrigger(startWork, DateTime.Now).IsAccepted)
            {

            }


            startWork(DateTime.Now);



        }

        [Fact]
        public void Temp()
        {

        }




    }
}
