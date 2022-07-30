using System;
using System.Linq;
using Telegram.Bot.Types;

namespace Xakpc.FeedbackBots.Helpers
{
    static class LogHelper
    {
        public static string MessageVerbose(Message confirmMessage)
        {
            return
                $"{confirmMessage.From?.FirstName} sent message {confirmMessage.MessageId} " +
                $"to chat {confirmMessage.Chat?.Id} at {confirmMessage.Date}. " +
                $"It is a reply to message {confirmMessage.ReplyToMessage?.MessageId} " +
                $"and has {confirmMessage.Entities?.Length} message entities.";
        }
    }
}
