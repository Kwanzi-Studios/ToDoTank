using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using ToDoTank.Models;
using System.Windows;  // for MessageBox

namespace ToDoTank.Services
{
    public class DataService
    {
        private readonly string _filePath;

        public DataService(string filePath = "ToDoTankData.json")
        {
            _filePath = filePath;
        }

        public void Save(List<string> categories, List<TodoItem> tasks)
        {
            try
            {
                var data = new SaveData
                {
                    Categories = new List<string>(categories),
                    Tasks = new List<SavedTask>()
                };

                foreach (var task in tasks)
                {
                    data.Tasks.Add(new SavedTask
                    {
                        Text = task.Text ?? "",
                        Category = task.Category ?? "General",
                        IsCompleted = task.IsCompleted,
                        IsPriority = task.IsPriority   // ← add this
                    });
                }

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed: {ex.Message}", "Error");
            }
        }

        public (List<string> Categories, List<TodoItem> Tasks) Load()
        {
            if (!File.Exists(_filePath))
                return (new List<string> { "General" }, new List<TodoItem>());

            try
            {
                string json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<SaveData>(json);
                if (data == null)
                    return (new List<string> { "General" }, new List<TodoItem>());

                var categories = data.Categories;
                if (!categories.Contains("General")) categories.Insert(0, "General");

                var tasks = data.Tasks.ConvertAll(t => new TodoItem
                {
                    Text = t.Text,
                    Category = t.Category,
                    IsCompleted = t.IsCompleted,
                    IsPriority = t.IsPriority      // ← add this
                });

                return (categories, tasks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Load failed: {ex.Message}", "Error");
                return (new List<string> { "General" }, new List<TodoItem>());
            }
        }
    }

    internal class SaveData
    {
        public List<string> Categories { get; set; } = new();
        public List<SavedTask> Tasks { get; set; } = new();
    }

    internal class SavedTask
{
    public string Text { get; set; } = "";
    public string Category { get; set; } = "";
    public bool IsCompleted { get; set; }
    public bool IsPriority { get; set; }   // ← add this
}
}