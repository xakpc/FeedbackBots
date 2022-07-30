using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Xakpc.FeedbackBots.Helpers;

namespace Xakpc.FeedbackBots.Services
{
    public class MasterBotService
    {
        private const string NewEmoji = "🆕";
        private const string DoneEmoji = "✅";

        readonly string _masterToken;
        readonly Database _database;

        public MasterBotService(Database database)
        {
            _database = database;
            _masterToken = EnvironmentHelper.GetEnvironmentVariable("MasterToken");
        }

        public async Task ResendMessageAsync(long clientId, string clientToken, Message message)
        {
            // get master bot user by token
            long chatId = await _database.GetMasterBotFromId(clientToken);

            if (chatId == default)
            {
                return; // no master bot user, nowhere to resend
            }

            // resend message to master bot
            var botClient = new TelegramBotClient(_masterToken);

            Message sentMessage = null;

            string top = $"{NewEmoji} Forwarded from {GetUserTag(message.From)}";

            // todo: enable photo resend - disabled until ban implemented to prevent abuse
            //if (message.Photo != null) // var resendPhoto
            //{
            //    var photo = message.Photo.Last();

            //    sentMessage = await botClient.SendPhotoAsync(
            //        chatId: chatId,
            //        photo: photo.FileId,
            //        caption: $"{top}/n{message.Caption}");
            //}

            //if (message.Document != null) // resend document
            //{
            //    sentMessage = await botClient.SendDocumentAsync(
            //        chatId: chatId,
            //        document: message.Document.FileId,
            //        caption: $"{top}/n{message.Caption}");
            //}

            if (message.Text != null)
            {                
                var messageText = $"{top}\n{EscapeText(message.Text)}";                               

                sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: messageText,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
                    disableWebPagePreview: true);
            }

            // save all message info
            var messageReferenceId = await _database.SaveClientMessageReference(clientToken, message.From.Id, message.MessageId, sentMessage.MessageId);
            
            // todo: implement ban and enable keyboard
            //InlineKeyboardMarkup inlineKeyboard = null;
            //    new(new[]
            //{
            //    // first row
            //    new []
            //    {
            //        //InlineKeyboardButton.WithCallbackData(text: "PromptReply", callbackData: $"reply:{messageReferenceId}"),
            //        //InlineKeyboardButton.WithCallbackData(text: "Block", callbackData: $"block:{message.From.Id}"),
            //    }
            //});
            //await botClient.EditMessageReplyMarkupAsync(chatId, sentMessage.MessageId, inlineKeyboard);
        }

        private string EscapeText(string text)
        {
            foreach (var item in Regex.Matches(text, @"[_*\[\]\(\)~`>#+\-=|{}.!]{1}").Select(i => i.Value).Distinct())
            {
                text = text.Replace(item, $@"\{item}");
            }
            
            return text;
        }
        
        private string GetUserTag(User from)
        {
            if (!string.IsNullOrEmpty(from.Username))
            {
                return $"@{from.Username}";
            }

            if (!string.IsNullOrEmpty(from.FirstName) || !string.IsNullOrEmpty(from.LastName))
            {
                return $"{from.FirstName} {from.LastName}".Trim();
            }
            
            //[inline mention of a user] (tg://user?id=123456789)
            return $"[no\\-name](tg://user?id={from.Id})";
        }

        internal async Task ResponseAsync(MasterBotResponse message, string userToken = default)
        {
            var botClient = new TelegramBotClient(userToken ?? _masterToken);

            if (!string.IsNullOrEmpty(message.PhotoId))
            {
                await botClient.SendPhotoAsync(message.FromId, new InputOnlineFile(message.PhotoId), caption: message.Text);
                return;
            }

            var sentMessage = await botClient.SendTextMessageAsync(
                chatId: message.FromId,
                text: message.Text,
                parseMode: message.Markdown.HasValue && message.Markdown.Value ? Telegram.Bot.Types.Enums.ParseMode.MarkdownV2 : null,
                disableWebPagePreview: message.DisableWebPagePreview,
                replyMarkup: message.Inline);

            Console.WriteLine(LogHelper.MessageVerbose(sentMessage));
        }

        public async Task BanAsync(long chatId, Message message)
        {
            var botClient = new TelegramBotClient(_masterToken);

            // todo
            throw new NotImplementedException();
        }

        #region Webhook Setup

        public Task<User> GetBot(string botToken)
        {
            var botClient = new TelegramBotClient(botToken);
            return botClient.GetMeAsync();
        }

        public Task Setup()
        {
            var botClient = new TelegramBotClient(_masterToken);
            var uri = EnvironmentHelper.GetEnvironmentVariable("Uri");
            var key = EnvironmentHelper.GetEnvironmentVariable("ClientId");
            return botClient.SetWebhookAsync($"https://{uri}/api/webhook?clientid={key}");
        }

        internal Task Setup(long id, string botToken)
        {
            var botClient = new TelegramBotClient(botToken);
            var uri = EnvironmentHelper.GetEnvironmentVariable("Uri");
            var key = EnvironmentHelper.GetEnvironmentVariable("ClientId");
            return botClient.SetWebhookAsync($"https://{uri}/api/{id}/{botToken}/webhook?clientid={key}");
        }

        public Task UnSetup(string botToken)
        {
            var botClient = new TelegramBotClient(botToken);
            return botClient.DeleteWebhookAsync();
        }

        public Task UnSetup()
        {
            var botClient = new TelegramBotClient(_masterToken);
            return botClient.DeleteWebhookAsync();
        }

        #endregion

        #region Misc Methods
        internal Task RemoveMessage(long chatId, int messageId)
        {
            try
            {
                var botClient = new TelegramBotClient(_masterToken);
                return botClient.DeleteMessageAsync(chatId, messageId);
            }
            catch (Exception)
            {
                // too old message could not be deleted, ignore that
                return Task.CompletedTask;
            }
        }

        internal async Task SetAnsweredAsync(Message message)
        {
            var botClient = new TelegramBotClient(_masterToken);
            var replyToMessage = message.ReplyToMessage;
            
            if (replyToMessage.Text.StartsWith(DoneEmoji))
            {
                return; // no need to update message
            }

            var edited = await botClient.EditMessageTextAsync(message.Chat.Id, replyToMessage.MessageId, replyToMessage.Text.Replace(NewEmoji, DoneEmoji));
            Console.WriteLine(LogHelper.MessageVerbose(edited));
        }

        internal Task AnswerCallback(string callbackId)
        {
            var botClient = new TelegramBotClient(_masterToken);
            return botClient.AnswerCallbackQueryAsync(callbackId);
        }

        internal Task SendWaitingAsync(long id)
        {
            var botClient = new TelegramBotClient(_masterToken);
            return botClient.SendChatActionAsync(id, Telegram.Bot.Types.Enums.ChatAction.Typing);
        }

        internal async Task<string> UploadFile(long fromId, string botName, byte[] qrCodeAsPngByteArr)
        {
            var botClient = new TelegramBotClient(_masterToken);

            await using Stream stream = new MemoryStream(qrCodeAsPngByteArr);
            Message message = await botClient.SendDocumentAsync(
                chatId: fromId,
                document: new InputOnlineFile(content: stream, fileName: $"{botName}.png"),
                caption: $"Invite QR Code for @{botName}");

            return message.Document.FileId;
        }
        #endregion
    }
}
