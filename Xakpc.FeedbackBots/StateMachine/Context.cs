using System;

namespace Xakpc.FeedbackBots.StateMachine
{
    public class Context
    {
        // A reference to the current state of the Context.
        public State State { get; set; }

        public Context(State state)
        {
            TransitionTo(state);
        }

        // The Context allows changing the State object at runtime.
        public void TransitionTo(State state)
        {
            Console.WriteLine($"Context: Transition to {state.GetType().Name}.");
            State = state;
            State.SetContext(this);
        }

        // The Context delegates part of its behavior to the current State
        // object.
        public StateAction GetAction(string messageText) => State?.GetAction(messageText);
    }
}