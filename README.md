# FeedbackBots

Feedback Bots allows you to deploy a feedback chatbot with master chatbot. 
Built on Azure Durable Functions as a part of [PlanetScale](http://planetscale.com) and [Hashnode](http://hashnode.com) hackathon.
Read an article how it was built [here](https://xakpc.info/building-telegram-chatbots-with-azure-durable-functions)

### Deploy
1. Deploy Feedback bots to Azure, open Azure Function App configuration
1. Create Database tables from `Database.sql` in MySQL-compatible database

### Setup Azure Function App
1. Set `Uri` to a FQDN of your Azure Function App (**without https://**)
1. Set `ConnectionString:Default` to a connection string of your MySQL-compatible database
1. Create a Host key for your functions in `App Keys` blade of your Azure Function App
1. Set `ClientId` to a created key - it will be used to auth webhooks

### Setup Master Bot
1. Create a Master Bot using [@BotFather](https://t.me/BotFather)
1. Set `MasterToken` setting to the chatbot token obtained from BotFather
1. Execute `api/setup` function with admin key - this will setup webhook for your master bot
1. Send `/start` to master bot, check logs of `MasterBotWebhook` for message like `Got message: User sent message 123 to chat **123456789** at 30.07.2022 22:23:14.`
1. Set `MasterChatId` to your chat number id

### Setup Client Bots
Now your Master Bot is operational and you are ready to add client bots
1. Create a Client Bot using [@BotFather](https://t.me/BotFather)
1. Send `/add` command to Master Bot
1. Send client bot's token to a master bot, the confirmation message should return

Setup is done, now any message to Client Bot will be forwarded to a Master Bot. 
Reply on Message in Master Bot to send back answer.

## SaaS version

Feedback bots availibe as SaaS solution (pre-deploed and pre-setup for you) on [FeedbackBots.com](http://feedbackbots.com)

## Configuration

The next configuration required to functions properly operate

Example of `local.settings.json`
```
{
  "IsEncrypted": false,
  "Values": {
    "Uri": "uri for webhooks",
    "MasterToken": "user-token for MasterBot",
    "MasterChatId": "your chat-id in MasterBot",
    "ClientId": "local"
  },

  "ConnectionStrings": {
    "Default": "MySql Connection String"
  }
}
```