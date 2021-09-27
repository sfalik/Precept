using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace StateMachine.Tests
{

    enum Status
    {
        New, Planned, Approved, WorkStarted, Completed, Closed
    }

    record Approval(string ApprovedBy, DateTime ApprovedOn);

    class WorkOrder : IStateful<Status>
    {
        private IStateMachine<Status> _stateMachine;
        public Status State => _stateMachine.State;
        public IReadOnlyList<Status> States => _stateMachine.States;
        public IReadOnlyList<IEvent<Delegate>> Events => _stateMachine.Events;

        public Approval? Approval { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
        public DateTime? ClosedOn { get; set; }

        public WorkOrder()
        {
            CreatedBy = "Shane Falik";
            CreatedOn = DateTime.Now;


            AsyncTransitionAction reopenLogic = async () => await Task.CompletedTask;

            _stateMachine = StateMachine.CreateBuilder<Status>()
                .DefineEvent(out var markAsPlanned)
                    .WhenStateIs(Status.New)
                    .TransitionTo(Status.Planned)

                .DefineEvent<Approval>(out var approve)
                    .WhenStateIs(Status.Planned)
                    .If(approval => approval.ApprovedBy != CreatedBy, "Cannot approve your own work orders")
                    .Execute(approval => Approval = approval)
                    .ThenTransitionTo(Status.Approved)

                .DefineEvent(out var reportHours)
                    .WhenStateIs(Status.Approved)
                    .TransitionTo(Status.WorkStarted)
                    .WhenStateIs(Status.WorkStarted, Status.Completed)
                    .KeepSameState()

                .DefineEvent(out var complete)
                    .WhenStateIs(Status.Approved, Status.WorkStarted)
                    .Execute(() => CompletedOn = DateTime.Now)
                    .ThenTransitionTo(Status.Completed)

                .DefineEvent(out var close)
                    .WhenStateIs(Status.Completed)
                    .Execute(() => ClosedOn = DateTime.Now)
                    .ThenTransitionTo(Status.Closed)

                .DefineAsyncEvent(out var reopen)
                    .WhenStateIs(Status.Closed)
                    .Execute(async () =>
                        {
                            CompletedOn = null;
                            ClosedOn = null;
                            await Task.CompletedTask;
                        })
                    .ThenTransitionTo(Status.WorkStarted)

                .Build(Status.New);

        }
    }

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

            if (approve.Evaluate().IsAccepted)
                approve.Trigger();

            if (close.Evaluate("Because").IsAccepted)
                await close.Trigger("because");

            if (startWork.Evaluate(DateTime.Now, out var newState, out string? reason))
            {
                startWork.Trigger(DateTime.Now);
                workflow.State.Should().Be(newState, "because the new state should match what was returned from the evaluation");
                reason.Should().BeNull("because a reason is not needed when the state is accepted");
            }
            else
            {
                newState.Should().Be(default(Status));
                reason.Should().NotBeNullOrWhiteSpace("because if an event cannot be triggered, and explaination should always be returend");
            }


        }

        [Fact]
        public void Temp()
        {
            default(Status).Should().Be(Status.New);
        }




    }
}
