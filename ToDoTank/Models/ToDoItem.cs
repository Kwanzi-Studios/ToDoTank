// Updated: Models/TodoItem.cs (add this property — shown for completeness, though not directly requested)
namespace ToDoTank.Models
{
    public class TodoItem
    {
        public string? Text { get; set; }
        public string? Category { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsPriority { get; set; }

        // New: Persists individual random variance offset for this task
        // Nullable for backward compatibility with old save files
        public double? VarianceOffset { get; set; }
    }
}