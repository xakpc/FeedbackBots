using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Threading.Tasks;
using Xakpc.FeedbackBots.Services;

namespace Xakpc.FeedbackBots
{
    public interface IDurableUserState
    {
        Task<States> GetUserState();

        void SetUserState(States state);

        void SetReplyToMessageId(long messageReferenceId);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class DurableUserState : IDurableUserState
    {
        [JsonProperty("State")]
        public States State { get; set; }
        public Task<States> GetUserState() => Task.FromResult(State);
        public void SetUserState(States state) => State = state;

        [JsonProperty("MessageReferenceId")]
        public long MessageReferenceId { get; private set; }

        [FunctionName(nameof(DurableUserState))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
        {
            if (!ctx.HasState)
            {
                ctx.SetState(new DurableUserState() { State = States.Main }); //todo figure out init
            }

            return ctx.DispatchAsync<DurableUserState>();
        }

        public void SetReplyToMessageId(long messageReferenceId)
        {
            MessageReferenceId = messageReferenceId;
        }
    }
}