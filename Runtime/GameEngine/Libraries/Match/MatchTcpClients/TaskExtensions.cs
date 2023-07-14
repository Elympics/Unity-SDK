using System;
using System.Threading.Tasks;

namespace MatchTcpClients
{
    public static class TaskExtensions
    {
        public static async Task CatchOperationCanceledException(this Task task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            { }
        }
    }
}
