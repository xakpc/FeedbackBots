using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xakpc.FeedbackBots.Services;

[assembly: FunctionsStartup(typeof(Xakpc.FeedbackBots.Startup))]

namespace Xakpc.FeedbackBots
{
    public class Startup : FunctionsStartup
    {       
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddTransient<MySqlConnection>(_ => new MySqlConnection(Environment.GetEnvironmentVariable("ConnectionStrings:Default")));
        
            builder.Services.AddSingleton<MasterBotService>();
            builder.Services.AddSingleton<ContextFactory>();
            builder.Services.AddSingleton<Database>();
            builder.Services.AddSingleton<IMessageSerializerSettingsFactory, CustomMessageSerializerSettingsFactory>();
        }

        /// <summary>
        /// A factory that provides the serialization for all inputs and outputs for activities and
        /// orchestrations, as well as entity state.
        /// </summary>
        internal class CustomMessageSerializerSettingsFactory : IMessageSerializerSettingsFactory
        {
            public JsonSerializerSettings CreateJsonSerializerSettings()
            {
                return new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    DateParseHandling = DateParseHandling.None,         
                };
            }
        }
    }
}
