using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xakpc.FeedbackBots.Helpers;
using Xakpc.FeedbackBots.Services;

namespace Xakpc.FeedbackBots
{
    public class WebhookFunctions
    {
        private readonly MasterBotService _masterBotService;
        readonly Database _database;

        public WebhookFunctions(MasterBotService masterBotService, Database database)
        {
            _database = database;
            _masterBotService = masterBotService;
        }

        [FunctionName(nameof(MasterBotWebhook))]
        public async Task<IActionResult> MasterBotWebhook(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "webhook")] HttpRequest req,
            [DurableClient] IDurableClient durableClient,
            ILogger log)
        {
            log.LogInformation("MasterBotWebhook function processed a webhook.");

            // Function input comes from the request content.            
            var update = await GetUpdateFrom(req);

            // Recieved message, prepare response according to state
            if (update.Message != null)
            {
                log.LogInformation("Got message: " + LogHelper.MessageVerbose(update.Message));

                await _masterBotService.SendWaitingAsync(update.Message.From.Id);

                var masterUser = long.Parse(Environment.GetEnvironmentVariable("MasterChatId"));
                if (update.Message.From.Id != masterUser)
                {
                    return new OkResult();
                }

                // the message is a reply, so we process it into reply state
                if (update.Message.ReplyToMessage != null)
                {
                    var reply = update.Message.ReplyToMessage;
                    var messageReference = await _database.GetClientMessageReference(messageId: reply.MessageId);

                    if (messageReference == null)
                    {
                        log.LogError("Message to answer not found");
                        return new OkResult();
                    }

                    // change user to responding state
                    var userEntity = new EntityId(nameof(DurableUserState), update.Message.From.Id.ToString());
                    await durableClient.SignalEntityAsync<IDurableUserState>(userEntity, us => us.SetUserState(States.Responding));

                    // store original message id                    
                    await durableClient.SignalEntityAsync<IDurableUserState>(userEntity, us => us.SetReplyToMessageId(messageReference.Id));
                }

                //Start MessageProcessFunctions Orchestration function
                string instanceId = await durableClient.StartNewAsync(OrchestrationFunctions.MessageProcessFunctions, update.Message);
                log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            }

            return new OkResult();
        }

        [FunctionName(nameof(ClientBotWebhook))]
        public async Task<IActionResult> ClientBotWebhook(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "{clientId:long}/{token}/webhook")] HttpRequest req,
            long clientId, string token,
            ILogger log)
        {
            log.LogInformation("ClientBotWebhook function processed a webhook from {clientId}/{token}.", clientId, token);

            // Function input comes from the request content.            
            var update = await GetUpdateFrom(req);

            if (await _database.UserBlocked(update.Message.From.Id, token))
            {
                log.LogInformation("User {FromId} is blocked", update.Message.From);
                return new OkResult();
            }

            // Recieved message, forward it to master bot
            if (update.Message != null)
            {
                log.LogInformation("Got message: " + LogHelper.MessageVerbose(update.Message));

                if (string.IsNullOrEmpty(update.Message.Text))
                {
                    await _masterBotService.ResponseAsync(new MasterBotResponse("This type of message not supported at the moment", update.Message.From.Id),
                        token);
                    return new OkResult();
                }

                if (update.Message.Text.Equals("/start", StringComparison.OrdinalIgnoreCase))
                {
                    await _masterBotService.ResponseAsync(new MasterBotResponse("Hello! Simply send me a message, I will try to get response for it.", update.Message.From.Id), 
                        token); // todo: welcome message for client should be customised
                    return new OkResult(); 
                }

                await _masterBotService.ResendMessageAsync(clientId, token, update.Message);
            }

            return new OkResult();
        }

        [FunctionName("Setup")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Admin, "get", Route = null)] HttpRequest req, ILogger log)
        {
            await _masterBotService.Setup();
            return new OkObjectResult("ok");
        }

        private static async Task<Update> GetUpdateFrom(HttpRequest req)
        {
            string requestBody = string.Empty;
            using (var streamReader = new StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }
            var update = JsonConvert.DeserializeObject<Update>(requestBody);
            return update;
        }
    }
}