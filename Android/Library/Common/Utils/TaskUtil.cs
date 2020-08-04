using System;
using System.Threading.Tasks;

namespace Library.Common.Utils
{
    internal static class TaskUtil
    {
        public static void RunAndForget(Func<Task> func) => Task
            .Factory
            .StartNew(func, TaskCreationOptions.LongRunning)
            .ConfigureAwait(false);
    }
}