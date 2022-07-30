using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xakpc.FeedbackBots.StateMachine;

namespace Xakpc.FeedbackBots.Services
{
    public enum States
    {
        Main,
        Add,
        Responding
    }

    public class ContextFactory
    {
        public Context RestoreContext(States userState)
        {
            var state = RestoreState(userState) ?? RestoreState(States.Main);
            var context = new Context(state);
            return context;            
        }

        private State RestoreState(States state)
        {
            switch (state)
            {
                case States.Main:
                    return new MainState();
                case States.Add:
                    return new AddState();
                case States.Responding:
                    return new RespondingState();
                default:
                    return null;
            }
        }
    }
}
