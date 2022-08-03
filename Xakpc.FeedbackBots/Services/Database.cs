using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xakpc.FeedbackBots.Services
{
    public interface IDatabase
    {
        Task<MasterBotUser> NewMasterUser(long fromId, string userName);
        Task AddUserTokenAsync(long fromId, string botToken, long chatId, string botName);

        Task<string> GetClientBotToken(long clientBotChatId);
        Task<string> GetClientBotName(long fromId);
        Task<long> GetMasterBotChatId(string clientToken);
        Task<long> GetMasterBotFromId(string clientToken);
        Task<string> GetMasterBotToken(long fromId);

        Task<long> SaveClientMessageReference(string clientToken, long clientId, int messageId, int resendMessagId);
        Task<ClientBotsMessageReference> GetClientMessageReference(long? messageReferenceId = default, long? messageId = default);

        Task<bool> HasMessageLeft(long fromId);
        Task ConsumeMessage(long fromId);
    }

    public class Database : IDatabase
    {
        readonly MySqlConnection _connection;

        public Database(MySqlConnection mySqlConnection)
        {
            _connection = mySqlConnection;
        }

        public async Task<MasterBotUser> NewMasterUser(long fromId, string userName)
        {
            var masterBot = new MasterBotUser
            {
                FromId = fromId,
                Username = userName,
            };

            string sqlQuery = "INSERT IGNORE INTO MasterBotUsers (FromId, Username) VALUES(@FromId, @Username)";
            await _connection.ExecuteAsync(sqlQuery, masterBot);

            return masterBot;
        }

        public Task AddUserTokenAsync(long fromId, string botToken, long chatId, string botName)
        {
            var clientBot = new ClientBot
            {
                ChatId = chatId,
                Token = botToken,
                MasterFromId = fromId,
                Name = botName
            };

            string sqlQuery = "INSERT IGNORE INTO ClientBots (ChatId, Name, Token, MasterFromId) VALUES(@ChatId, @Name, @Token, @MasterFromId)";
            return _connection.ExecuteAsync(sqlQuery, clientBot);
        }

        public async Task<long> SaveClientMessageReference(string clientToken, long originalFromId, int originaMessageId, int resendMessagId)
        {
            try
            {
                var messageRef = new ClientBotsMessageReference
                {
                    OriginalMessageId = originaMessageId,
                    OriginalFromId = originalFromId,
                    ClientBotChatId = await GetMasterBotChatId(clientToken),

                    IsAnswered = false,
                    ResendMessageId = resendMessagId,
                };

                // insert new client message ref
                string sqlQuery = @"INSERT INTO ClientBotsMessageReference (OriginalMessageId, OriginalFromId, ClientBotChatId, IsAnswered, ResendMessageId) 
                    VALUES(@OriginalMessageId, @OriginalFromId, @ClientBotChatId, @IsAnswered, @ResendMessageId);";
                await _connection.ExecuteAsync(sqlQuery, messageRef);

                // get latest ID
                var result = await _connection.ExecuteScalarAsync<long>("SELECT Id FROM ClientBotsMessageReference WHERE " +
                    "OriginalMessageId = @OriginalMessageId AND OriginalFromId = @OriginalFromId AND ResendMessageId = @ResendMessageId",
                    new { messageRef.OriginalMessageId, messageRef.OriginalFromId, messageRef.ResendMessageId });

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public Task<string> GetClientBotName(long fromId)
        {
            var sql = "SELECT Name FROM ClientBots WHERE MasterFromId = @FromId;";
            return _connection.QuerySingleAsync<string>(sql, new { FromId = fromId });
        }

        public Task<ClientBotsMessageReference> GetClientMessageReference(long? messageReferenceId = default, long? messageId = default)
        {
            try
            {
                if (messageReferenceId != default)
                {
                    var sql = "SELECT * FROM ClientBotsMessageReference WHERE Id = @Id";
                    return _connection.QueryFirstOrDefaultAsync<ClientBotsMessageReference>(sql, new { Id = messageReferenceId });
                }
                else if (messageId != default)
                {
                    var sql = "SELECT * FROM ClientBotsMessageReference WHERE ResendMessageId = @Id";
                    return _connection.QueryFirstOrDefaultAsync<ClientBotsMessageReference>(sql, new { Id = messageId });
                }
                else
                {
                    return Task.FromResult<ClientBotsMessageReference>(null);
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult<ClientBotsMessageReference>(null);
            }
        }

        public Task<long> GetMasterBotFromId(string clientToken)
        {
            var sql = "SELECT MasterFromId FROM ClientBots WHERE Token = @Token;";
            return _connection.QuerySingleAsync<long>(sql, new { Token = clientToken });
        }

        public Task<long> GetMasterBotChatId(string clientToken)
        {
            var sql = "SELECT ChatId FROM ClientBots WHERE Token = @Token;";
            return _connection.QuerySingleAsync<long>(sql, new { Token = clientToken });
        }

        public Task<string> GetMasterBotToken(long fromId)
        {
            var sql = "SELECT Token FROM ClientBots WHERE MasterFromId = @FromId;";
            return _connection.QuerySingleAsync<string>(sql, new { FromId = fromId });
        }

        public Task<string> GetClientBotToken(long clientBotChatId)
        {
            var sql = "SELECT Token from ClientBots where ChatId = @ClientBotChatId;";
            return _connection.QuerySingleAsync<string>(sql, new { ClientBotChatId = clientBotChatId });
        }

        public Task<bool> HasMessageLeft(long fromId)
        {            
            return Task.FromResult(true);
        }

        public Task ConsumeMessage(long fromId)
        {
            return Task.CompletedTask;
        }
        
        public async Task BlockUser(long clientBotChatId, long originalFromId)
        {
            var masterBot = new BlockedUser
            {
                OriginalFromId = originalFromId,
                ClientBotChatId = clientBotChatId
            };

            string sqlQuery = "INSERT IGNORE INTO BlockedUsers (OriginalFromId, ClientBotChatId) VALUES(@OriginalFromId, @ClientBotChatId)";
            await _connection.ExecuteAsync(sqlQuery, masterBot);
        }

        public Task<IEnumerable<int>> GetAllMessages(long clientBotChatId, long originalFromId)
        {
            var sql = "SELECT ResendMessageId FROM ClientBotsMessageReference WHERE ClientBotChatId = @ClientBotChatId AND OriginalFromId= @OriginalFromId;";
            return _connection.QueryAsync<int>(sql, new { ClientBotChatId = clientBotChatId, OriginalFromId = originalFromId });
        }
        
        public Task<string> GetFileId(long fromId)
        {
            var sql = "SELECT InviteFileId FROM ClientBots WHERE MasterFromId = @FromId;";
            return _connection.QuerySingleOrDefaultAsync<string>(sql, new { FromId = fromId });
        }

        public Task SaveFileId(long fromId, string fileId)
        {
            var sql = "UPDATE ClientBots SET InviteFileId = @FileId WHERE MasterFromId = @FromId;";
            return _connection.ExecuteAsync(sql, new { FileId = fileId, FromId = fromId });
        }
        
        public async Task<bool> UserBlocked(long originalFromId, string clientToken)
        {
            var chatId = await GetMasterBotChatId(clientToken);

            var sql = "SELECT COUNT(Id) FROM BlockedUsers WHERE OriginalFromId = @OriginalFromId AND ClientBotChatId = @ChatId";
            var count = await _connection.ExecuteScalarAsync<int>(sql, new { ChatId = chatId, OriginalFromId = originalFromId });
            return count > 0;
        }
    }
}
