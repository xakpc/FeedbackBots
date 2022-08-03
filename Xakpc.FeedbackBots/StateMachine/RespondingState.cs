using System;
using Xakpc.FeedbackBots.Services;

namespace Xakpc.FeedbackBots.StateMachine
{
    public class RespondingState : State
    {
        public override States Type => States.Responding;

        public override StateAction GetAction(string messageText)
        {
            var message = messageText.Trim();

            if (message.Equals("/cancel", StringComparison.OrdinalIgnoreCase))
            {
                _context.TransitionTo(new MainState());
                return new StateAction(new MasterBotResponse("Cancelled"));
            }          

            var ms = new MainState();
            _context.TransitionTo(ms);

            if (message.StartsWith("/block"))
            {
                return new StateAction(Activity: nameof(MasterBotActivityFunctions.BlockUser));
            }

            if (message.StartsWith("/"))
            {
                return ms.GetAction(messageText);
            }

            return new StateAction(Activity: nameof(MasterBotActivityFunctions.ActivityResponse));
        }
    }
}