namespace ToDoTank.Models
{
    public class TodoItem
    {
        public string? Text { get; set; }
        public string? Category { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsPriority { get; set; }
    }
}