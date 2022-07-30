using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Xakpc.FeedbackBots.Services;

namespace Xakpc.FeedbackBots
{
    public record class MasterBotResponse(string Text, long? FromId = default,
        InlineKeyboardMarkup Inline = default, bool? Markdown = default, bool? DisableWebPagePreview = default,
        string PhotoId = default);

    public class OrchestrationFunctions
    {
        public OrchestrationFunctions(MasterBotService masterBotService, ContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _masterBotService = masterBotService;
        }

        public const string MessageProcessFunctions = "MessageProcessFunctions";
        public const string SendResponseAction = "SendResponse";

        private readonly MasterBotService _masterBotService;
        private readonly ContextFactory _contextFactory;

        [FunctionName(MessageProcessFunctions)]
        public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var message = context.GetInput<Message>();

            // create unique id of DurableUserState based on user id
            var userEntity = new EntityId(nameof(DurableUserState), message.From.Id.ToString());
            var userProxy = context.CreateEntityProxy<IDurableUserState>(userEntity);

            // get user context (user state) from durable entity
            var userState = await userProxy.GetUserState();

            // restore context
            var userContext = _contextFactory.RestoreContext(userState);

            // do FSM step and get action to execute
            var (response, activity) = userContext.GetAction(message.Text);

            // save new state            
            userProxy.SetUserState(userContext.State.Type);

            // send pre-action response if any
            if (response != default)
            {
                await context.CallActivityAsync(SendResponseAction, response with { FromId = message.From.Id });
            }

            // execute action if any and get response
            if (!string.IsNullOrEmpty(activity))
            {
                var postActivityResponse = await context.CallActivityAsync<MasterBotResponse>(activity, message);

                // send post-action response if any 
                if (postActivityResponse != default)
                {
                    await context.CallActivityAsync(SendResponseAction, postActivityResponse with { FromId = message.From.Id });
                }
            }
        }

        [FunctionName(SendResponseAction)]
        public async Task SendResponse([ActivityTrigger] MasterBotResponse message, ILogger log)
        {
            try
            {
                await _masterBotService.ResponseAsync(message);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }
    }
}