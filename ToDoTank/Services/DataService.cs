// Full Updated: Services/DataService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ToDoTank.Models;

namespace ToDoTank.Services
{
    public class DataService
    {
        private readonly string _filePath;

        public DataService(string filePath = "ToDoTankData.json")
        {
            _filePath = filePath;
        }

        public void Save(SaveData data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Save failed: {ex.Message}", "Error");
            }
        }

        public SaveData Load()
        {
            if (!File.Exists(_filePath))
                return new SaveData();

            try
            {
                string json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<SaveData>(json);
                return data ?? new SaveData();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Load failed: {ex.Message}", "Error");
                return new SaveData();
            }
        }
    }

    public class SaveData
    {
        public List<string> Categories { get; set; } = new();
        public List<SavedTask> Tasks { get; set; } = new();

        // Customization settings (from previous persistence updates)
        public double DefaultFontSize { get; set; } = 24;
        public string? TextColorArgb { get; set; }
        public string? PriorityColorArgb { get; set; }
        public string? BackgroundColorArgb { get; set; }
    }

    public class SavedTask
    {
        public string Text { get; set; } = "";
        public string Category { get; set; } = "";
        public bool IsCompleted { get; set; }
        public bool IsPriority { get; set; }

        // New: Persist variance offset
        public double? VarianceOffset { get; set; }
    }
}