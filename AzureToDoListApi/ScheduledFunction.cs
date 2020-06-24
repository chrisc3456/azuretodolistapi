using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace AzureToDoListApi
{
    public static class ScheduledFunction
    {
        /**
         * Timer function executed according to the time period defined in the cron expression parameter to periodically delete completed tasks
         */
        [FunctionName("ScheduledFunction")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer,
            [Table("todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            ILogger log)
        {
            var query = new TableQuery<TodoTableEntity>();
            var segment = await todoTable.ExecuteQuerySegmentedAsync(query, null);
            var deleted = 0;

            foreach (var todo in segment)
            {
                if (todo.IsCompleted)
                {
                    await todoTable.ExecuteAsync(TableOperation.Delete(todo));
                    deleted++;
                }
            }

            log.LogInformation($"Deleted {deleted} items at: {DateTime.Now}");
        }
    }
}
