CREATE TABLE MasterBotUsers (
  FromId BIGINT NOT NULL PRIMARY KEY,
  Username varchar(255) NOT NULL,
  IsPro TINYINT NOT NULL default 1,
);

CREATE TABLE ClientBots (
  ChatId BIGINT NOT NULL PRIMARY KEY,
  Name varchar(255) NOT NULL,
  Token varchar(255) NOT NULL,  
  MasterFromId BIGINT,
  InviteFileId varchar(255)
  KEY master_from_id_idx (MasterFromId)
);

CREATE TABLE ClientBotsMessageReference (
  Id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  
  OriginalMessageId INT NOT NULL,
  OriginalFromId BIGINT NOT NULL,
  ResendMessageId INT NOT NULL,
  
  IsAnswered TINYINT NOT NULL default 0,
  ClientBotChatId BIGINT,
  KEY client_chat_id_idx (ClientBotChatId)
);

CREATE TABLE BlockedUsers (
  Id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  
  OriginalFromId BIGINT NOT NULL,
  
  ClientBotChatId BIGINT,
  KEY client_chat_id_idx (ClientBotChatId)
);