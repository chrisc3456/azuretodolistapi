using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;

namespace AzureToDoListApi
{
    public static class TodoApi
    {

        /**
         * POST api/todo
         */
        [FunctionName("CreateTodo")]
        public static async Task<IActionResult> CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] IAsyncCollector<TodoTableEntity> todoTable,
            [Queue("todos", Connection = "AzureWebJobsStorage")] IAsyncCollector<Todo> todoQueue,
            ILogger log)
        {
            log.LogInformation("Creating new todo list item");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Deserialise the request into a TodoCreateModel entity - note not using Todo as we don't want to allow changes to auto set fields (datetime and id)
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);
            var todo = new Todo() { TaskDescription = input.TaskDescription };

            // Add the entity to table storage and a message to the queue
            await todoTable.AddAsync(todo.ToTableEntity());
            await todoQueue.AddAsync(todo);

            return new OkObjectResult(todo);
        }


        /**
         * GET api/todo
         */
        [FunctionName("GetTodos")]
        public static async Task<IActionResult> GetToDos(
             [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            ILogger log)
        {
            log.LogInformation("Getting todo list items");

            var query = new TableQuery<TodoTableEntity>();
            var segment = await todoTable.ExecuteQuerySegmentedAsync(query, null);
            return new OkObjectResult(segment.Select(Mappings.ToTodo));
        }


        /**
         * GET api/todo/{id}
         */
        [FunctionName("GetTodoById")]
        public static IActionResult GetToDo(
             [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")] HttpRequest req,
            [Table("todos", "TODO", "{id}", Connection = "AzureWebJobsStorage")] TodoTableEntity todo,
            ILogger log, string id)
        {
            log.LogInformation("Getting todo list item " + id);

            if (todo == null)
            {
                log.LogInformation($"Item {id} not found");
                return new NotFoundResult();
            }

            return new OkObjectResult(todo.ToTodo());
        }


        /**
         * PUT api/todo/{id}
         */
        [FunctionName("UpdateToDo")]
        public static async Task<IActionResult> UpdateToDo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            ILogger log, string id)
        {
            log.LogInformation("Updating todo list item " + id);

            // Deserialise the request into a TodoUpdateModel entity - note not using TodoModel as we don't want to allow changes to auto set fields (datetime and id)
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);

            // Look up the existing record from the table
            var findOperation = TableOperation.Retrieve<TodoTableEntity>("TODO", id);
            var findResult = await todoTable.ExecuteAsync(findOperation);
            if (findResult.Result == null)
            {
                return new NotFoundResult();
            }

            // Create a variable for the entity and update the values
            var existingRow = (TodoTableEntity)findResult.Result;
            existingRow.IsCompleted = updated.IsCompleted;
            if (!string.IsNullOrEmpty(updated.TaskDescription))
            {
                existingRow.TaskDescription = updated.TaskDescription;
            }

            // Push the updated entity into the table with a replace operation
            var replaceOperation = TableOperation.Replace(existingRow);
            await todoTable.ExecuteAsync(replaceOperation);

            return new OkObjectResult(existingRow.ToTodo());
        }


        /**
         * DELETE api/todo/{id}
         */
        [FunctionName("DeleteTodo")]
        public static async Task<IActionResult> DeleteToDo(
             [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            ILogger log, string id)
        {
            log.LogInformation("Deleting todo list item " + id);

            var deleteOperation = TableOperation.Delete(new TableEntity() { PartitionKey = "TODO", RowKey = id, ETag = "*" });  // ETag value forces delete regardless of current object version
            try
            {
                var deleteResult = await todoTable.ExecuteAsync(deleteOperation);
            }
            catch (StorageException e) when (e.RequestInformation.HttpStatusCode == 404)
            {
                return new NotFoundResult();
            }

            return new OkResult();      // Using OkResult instead of OkObjectResult as we have nothing to return
        }
    }
}
