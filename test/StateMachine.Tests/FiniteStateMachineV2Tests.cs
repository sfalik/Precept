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
                    .TransitionTo(Status.Approved)
                    .WhenStateIs(Status.Approved)
                    .Execute(() => { })
                    .AndKeepSameState()

                .DefineAsyncEvent<string>(out var close)
                    .WhenStateIs(Status.Completed)
                    .Execute(async reason => await Task.CompletedTask)
                    .ThenTransitionTo(Status.Closed)

                .DefineEvent<DateTime>(out var startWork)
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

            if (close.IsAccepted("Because"))
                await close.Trigger("because");

            if (startWork.IsAccepted(DateTime.Now))
                startWork.Trigger(DateTime.Now);





        }

        [Fact]
        public void Temp()
        {

        }




    }
}
