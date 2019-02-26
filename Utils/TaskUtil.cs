using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Layer4StackTest")]

namespace Layer4Stack.Utils
{

    /// <summary>
    /// Task utility 
    /// </summary>
    internal static class TaskUtil
    {

        /// <summary>
        /// Run action as task 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static async Task<bool> RunAction(Action action, ILogger logger)
        {
            try
            {
                await Task.Run(action);
                return await Task.FromResult(true);
            } catch(Exception e)
            {
                logger.LogError("Execution error: {msg}", e.Message);
                return await Task.FromResult(false);
            }
        }

    }
}
