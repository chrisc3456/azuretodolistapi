using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace AzureToDoListApi
{
    public static class TodoApi
    {
        // Hold items in memory for now, to replace with database integration later
        static List<Todo> items = new List<Todo>();
        

        /**
         * POST api/todo
         */
        [FunctionName("CreateTodo")]
        public static async Task<IActionResult> CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req, ILogger log)
        {
            log.LogInformation("Creating new todo list item");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Deserialise the request into a TodoCreateModel entity - note not using Todo as we don't want to allow changes to auto set fields (datetime and id)
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);
            var todo = new Todo() { TaskDescription = input.TaskDescription };

            items.Add(todo);

            return new OkObjectResult(todo);
        }


        /**
         * GET api/todo
         */
        [FunctionName("GetTodos")]
        public static IActionResult GetToDos(
             [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")] HttpRequest req, ILogger log)
        {
            log.LogInformation("Getting todo list items");
            return new OkObjectResult(items);
        }


        /**
         * GET api/todo/{id}
         */
        [FunctionName("GetTodoById")]
        public static IActionResult GetToDo(
             [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")] HttpRequest req, ILogger log, string id)
        {
            log.LogInformation("Getting todo list item " + id);

            var todo = items.FirstOrDefault(t => t.Id == id);
            if (todo == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(todo);
        }


        /**
         * PUT api/todo/{id}
         */
        [FunctionName("UpdateToDo")]
        public static async Task<IActionResult> UpdateToDo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")] HttpRequest req, ILogger log, string id)
        {
            log.LogInformation("Updating todo list item " + id);

            var todo = items.FirstOrDefault(t => t.Id == id);
            if (todo == null)
            {
                return new NotFoundResult();
            }

            // Deserialise the request into a TodoUpdateModel entity - note not using TodoModel as we don't want to allow changes to auto set fields (datetime and id)
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);

            todo.IsCompleted = updated.IsCompleted;
            if (!string.IsNullOrEmpty(updated.TaskDescription))
            {
                todo.TaskDescription = updated.TaskDescription;
            }

            return new OkObjectResult(todo);
        }


        /**
         * DELETE api/todo/{id}
         */
        [FunctionName("DeleteTodo")]
        public static IActionResult DeleteToDo(
             [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")] HttpRequest req, ILogger log, string id)
        {
            log.LogInformation("Deleting todo list item " + id);

            var todo = items.FirstOrDefault(t => t.Id == id);
            if (todo == null)
            {
                return new NotFoundResult();
            }

            items.Remove(todo);
            return new OkResult();      // Using OkResult instead of OkObjectResult as we have nothing to return
        }
    }
}
