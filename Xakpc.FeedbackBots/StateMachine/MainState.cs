using System;
using Xakpc.FeedbackBots.Services;

namespace Xakpc.FeedbackBots.StateMachine
{
    // Concrete States implement various behaviors, associated with a state of
    // the Context.

    class MainState : State
    {
        public override States Type => States.Main;

        public override StateAction GetAction(string messageText)
        {
            switch (messageText)
            {
                case "/start":
                    return new StateAction(Response: new MasterBotResponse("Welcome to a feedbacks bot. Here is your instructions dog"), 
                        Activity: nameof(MasterBotActivityFunctions.ActivityRegisterMasterUser));
                case "/add":
                    _context.TransitionTo(new AddState());
                    return new StateAction(Response: new MasterBotResponse("Create client-facing chatbot through @BotFather and send me generated UserToken"));
                case "/remove":
                    return new StateAction(Activity: nameof(MasterBotActivityFunctions.ActivityDoRemove));
                case "/qr":
                    return new StateAction(Activity: nameof(MasterBotActivityFunctions.ActivityCreateQrCode));
            }

            return new StateAction(Response: new MasterBotResponse("Unknown command, press **Menu** button for list of commands", Markdown: true));
        }
    }
}