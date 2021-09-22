using System;
using Xunit;

namespace StateMachineV2.Tests
{

    enum Status
    {
        New, Planned, Approved, WorkStarted, Completed, Closed
    }

    public class FiniteStateMachineV2Tests
    {
        [Fact]
        public void Test1()
        {
            var workflow = FiniteStateMachine.CreateBuilder<Status>()
                .WhenStateIs(Status.New)
                .AndEventFired(out var approve)
                .TransitionTo(Status.Approved)

                .WhenStateIs(Status.Completed)
                .AndEventFired<string>(out var close)
                .Execute(reason => { })
                .ThenTransitionTo(Status.Closed)

                .WhenStateIs(Status.Planned, Status.Approved)
                .AndEventFired<DateTime>(out var startWork)
                .If(time => time >= DateTime.Now, "Because work cannot start in the past")
                    .Execute(time => { })
                    .ThenTransitionTo(Status.WorkStarted)


                .Build(Status.New)
            ;

            approve();
            close("because");

            workflow.IsEventAccepted(approve);
            workflow.IsEventAccepted(close, "test");

            startWork(DateTime.Now);
        }
    }
}
