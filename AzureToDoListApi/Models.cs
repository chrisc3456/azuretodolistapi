using Microsoft.Azure.Cosmos.Table;
using System;

namespace AzureToDoListApi
{
    public class Todo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("n");
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
        public string TaskDescription { get; set; }
        public bool IsCompleted { get; set; }
    }

    /**
     * Internal model class for creating model objects via the API. Fields restricted to only those which are allowed to be set on creation
     */
    public class TodoCreateModel
    {
        public string TaskDescription { get; set; }
    }

    /**
     * Internal model class for updating model objects via the API. Fields restricted to only those which are allowed to be set on update
     */
    public class TodoUpdateModel
    {
        public string TaskDescription { get; set; }
        public bool IsCompleted { get; set; }
    }

    /**
     * Entity model for persistent table storage of todo items
     */
    public class TodoTableEntity: TableEntity
    {
        public DateTime CreatedTime { get; set; }
        public string TaskDescription { get; set; }
        public bool IsCompleted { get; set; }
    }

    /**
     * Mappings for converting between table model entity types and application model for API requests/responses
     */
    public static class Mappings
    {
        public static TodoTableEntity ToTableEntity(this Todo todo)
        {
            return new TodoTableEntity()
            {
                PartitionKey = "TODO",
                RowKey = todo.Id,
                CreatedTime = todo.CreatedTime,
                IsCompleted = todo.IsCompleted,
                TaskDescription = todo.TaskDescription
            };
        }

        public static Todo ToTodo(this TodoTableEntity todo)
        {
            return new Todo()
            {
                Id = todo.RowKey,
                CreatedTime = todo.CreatedTime,
                TaskDescription = todo.TaskDescription,
                IsCompleted = todo.IsCompleted
            };
        }
    }
}
