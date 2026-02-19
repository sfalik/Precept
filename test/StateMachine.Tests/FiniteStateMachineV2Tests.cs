using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace StateMachine.Tests
{
    internal enum Status
    {
        New, Planned, Approved, WorkStarted, Completed, Closed, Cancelled
    }

    internal record Approval(string ApprovedBy, DateTime ApprovedOn);

    internal class WorkOrder : IStateful<Status>
    {
        private readonly IStateMachine<Status> _stateMachine;
        public Status State => _stateMachine.State;
        public IReadOnlyList<Status> States => _stateMachine.States;
        public IReadOnlyList<IEvent> Events => _stateMachine.Events;

        public Approval? Approval { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
        public DateTime? ClosedOn { get; set; }

        public Trigger MarkAsPlanned { get; init; }

 
        public WorkOrder()
        {
            CreatedBy = "Shane Falik";
            CreatedOn = DateTime.Now;

            _stateMachine = StateMachine.CreateBuilder<Status>()
                //.DefineAttribute<string>(ref CreatedBy)

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

            MarkAsPlanned = markAsPlanned.Trigger;

        }
    }

    public class FiniteStateMachineV2Tests
    {
        [Fact(Skip = "Not ready")]
        public async void Test1()
        {
            var workflow = StateMachine.CreateBuilder<Status>()
                //.DefineAttribute<string>(out var name)

                .DefineEvent(out var approve)
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
                        .And(time => time.Year < 2040, "can't plan that far ahead")
                            .Execute(time => { })
                            .ThenTransitionTo(Status.WorkStarted)
                        .Else.If(time => false, "arbitrary reason")
                            .Execute(time => { })
                            .AndKeepSameState()
                    .WhenStateIs(Status.Closed)
                        .TransitionTo(Status.Approved)

                .DefineEvent(out var cancel)
                    .RegardlessOfState()
                    .TransitionTo(Status.Cancelled)

                .Build(Status.New)
            ;

            if (approve.Test().IsAccepted)
            {
                approve.Trigger();
            }

            if (close.Test("Because").IsAccepted)
            {
                await close.Trigger("because");
            }

            if (startWork.Test(DateTime.Now, out var newState, out var reason))
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
