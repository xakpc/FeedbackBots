using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xakpc.FeedbackBots.Services;

namespace Xakpc.FeedbackBots.StateMachine
{
    public class AddState : State
    {
        public override States Type => States.Add;

        bool IsValidToken(string messageText)
        {
            return Regex.IsMatch(messageText, "[0-9]{9}:[a-zA-Z0-9_-]{35}");
        }

        public override StateAction GetAction(string messageText)
        {
            if (messageText.Equals("/cancel", StringComparison.OrdinalIgnoreCase))
            {
                _context.TransitionTo(new MainState());
                return new StateAction(new MasterBotResponse("Cancelled"));
            }

            if (IsValidToken(messageText))
            {
                _context.TransitionTo(new MainState());
                return new StateAction(Activity: nameof(MasterBotActivityFunctions.ActivityDoAdd));
            }

            return new StateAction(new MasterBotResponse("Invalid input, try again or /cancel"));
        }
    }
}