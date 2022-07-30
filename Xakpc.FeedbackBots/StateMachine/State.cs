using System;
using Xakpc.FeedbackBots.Services;

namespace Xakpc.FeedbackBots.StateMachine
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Response"></param>
    /// <param name="Activity"></param>
    public record StateAction(MasterBotResponse Response = default, string Activity = default);

    /// <summary>
    /// The base State class declares methods that all Concrete State should implement
    /// </summary>
    public abstract class State
    {
        /// <summary>
        /// Property should be implemented in concrete state for our factory to construct concrete type of state
        /// </summary>
        /// <remarks>
        /// It could be Type if we want to use reflection
        /// </remarks>
        public abstract States Type { get; }

        protected Context _context;

        /// <summary>
        /// a backreference to the Context object, associated with the State. 
        /// This backreference can be used by States to transition the Context to another State.
        /// </summary>
        public void SetContext(Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Get Activity Function name
        /// </summary>
        public abstract StateAction GetAction(string messageText);
    }
}