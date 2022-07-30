using System;
using System.Linq;

namespace Xakpc.FeedbackBots.Services
{
    /// <summary>
    /// User of Master Bot
    /// </summary>
    public class MasterBotUser
    {
        /// <summary>
        /// PK
        /// </summary>
        public long FromId { get; set; }
        public string Username { get; set; }
        public bool IsPro { get; set; }
    }

    /// <summary>
    /// Client Bot of Master Bot
    /// </summary>
    public class ClientBot
    {
        /// <summary>
        /// PK
        /// </summary>
        public long ChatId { get; set; }
        public string Name { get; set; }
        public string Token { get; set; }
        /// <summary>
        /// FK: MasterBotUser
        /// </summary>
        public long MasterFromId { get; set; }
    }

    /// <summary>
    /// Reference to message
    /// </summary>
    public class ClientBotsMessageReference
    {
        /// <summary>
        /// PK
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Id of the message in the client chat
        /// </summary>
        public int OriginalMessageId { get; set; }
        /// <summary>
        /// Id of the user in the client chat
        /// </summary>
        public long OriginalFromId { get; set; }
        public long ResendMessageId { get; set; }
        public bool IsAnswered { get; set; }
        /// <summary>
        /// FK: ClientBots
        /// </summary>
        public long ClientBotChatId { get; set; }
    }
}
