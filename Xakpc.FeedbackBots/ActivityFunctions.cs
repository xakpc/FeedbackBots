using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Xakpc.FeedbackBots.Helpers;
using Xakpc.FeedbackBots.Services;

namespace Xakpc.FeedbackBots
{
    public class MasterBotActivityFunctions
    {
        readonly Database _database;
        readonly MasterBotService _telegramService;

        public MasterBotActivityFunctions(Database database, MasterBotService telegramService)
        {
            _telegramService = telegramService;
            _database = database;
        }

        [FunctionName(nameof(ActivityRegisterMasterUser))]
        public async Task<MasterBotResponse> ActivityRegisterMasterUser([ActivityTrigger] Message message, ILogger log)
        {
            var chatId = message.From.Id;

            var user = await _database.NewMasterUser(chatId, message.From.Username);

            log.LogInformation("New master user {User}", message.From);

            return default; // no need to report response here
        }

        [FunctionName(nameof(ActivityDoAdd))]
        public async Task<MasterBotResponse> ActivityDoAdd([ActivityTrigger] Message message, ILogger log)
        {
            log.LogInformation($"Adding client bot for {message.From.Id}");
            var botToken = Regex.Match(message.Text, "[0-9]+:[a-zA-Z0-9_-]{35}").Value;
            var bot = await _telegramService.GetBot(botToken);

            await _database.AddUserTokenAsync(message.From.Id, botToken, bot.Id, bot.Username);
            await _telegramService.Setup(message.From.Id, botToken);

            return new MasterBotResponse($"Chatbot {bot.Username} attached as a client bot");
        }

        [FunctionName(nameof(ActivityDoRemove))]
        public async Task<MasterBotResponse> ActivityDoRemove([ActivityTrigger] Message message, ILogger log)
        {
            log.LogInformation($"Removing client bot for {message.From.Id}");

            var botToken = await _database.GetMasterBotToken(message.From.Id);
            var bot = await _telegramService.GetBot(botToken);
            await _telegramService.UnSetup(botToken);

            return new MasterBotResponse($"Chatbot {bot.Username} detached as client bot");
        }

        [FunctionName(nameof(ActivityCreateQrCode))]
        public async Task<MasterBotResponse> ActivityCreateQrCode([ActivityTrigger] Message message, ILogger log)
        {
            string botName = await _database.GetClientBotName(message.From.Id);
            string url = $"https://t.me/{botName}";

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);

            var fileId = await _telegramService.UploadFile(message.From.Id, botName, qrCodeAsPngByteArr);

            // todo: save this fileId to resend image without building it as new

            return new MasterBotResponse(string.Empty, PhotoId: fileId);
        }

        /// <summary>
        /// Response to client bot message from the master bot.
        /// </summary>
        [FunctionName(nameof(ActivityResponse))]
        public async Task<MasterBotResponse> ActivityResponse(
            [ActivityTrigger] Message message,
            [DurableClient] IDurableEntityClient client,
            ILogger log)
        {
            var fromId = message.From.Id;

            // validate user can response
            if(!await _database.HasMessageLeft(fromId))
            {
                return new MasterBotResponse("You out of avalible responces this month, /subscribe to get more");
            }

            // update message (icon)
            await _telegramService.SetAnsweredAsync(message);

            // get client chat messageRef and botToken
            var entityId = new EntityId(nameof(DurableUserState), fromId.ToString());
            var userState = await client.ReadEntityStateAsync<DurableUserState>(entityId);
            var messageRef = await _database.GetClientMessageReference(
                messageReferenceId: userState.EntityState.MessageReferenceId);
            var botToken = await _database.GetClientBotToken(messageRef.ClientBotChatId);

            // send responce to a client chat
            await _telegramService.ResponseAsync(
                new MasterBotResponse(message.Text, messageRef.OriginalFromId),
                botToken);

            await _database.ConsumeMessage(fromId);

            // no answer needed
            return default;
        }
    }
}
