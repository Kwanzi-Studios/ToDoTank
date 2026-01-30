using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ToDoTank.Models;
using ToDoTank.Services;

namespace ToDoTank
{
    public partial class MainWindow : Window
    {
        private readonly Random _random = new Random();
        private readonly List<string> _categories = new() { "Default" };
        private string _currentCategory = "Default";
        private readonly Dictionary<TextBlock, TodoItem> _taskElements = new();
        private readonly DataService _dataService = new DataService();
        private readonly AnimationService _animationService = new AnimationService();
        private readonly TrayService _trayService;

        private bool _isClosingConfirmed = false;
        private bool _minimizingToTray = false;

        public MainWindow()
        {
            InitializeComponent();
            _trayService = new TrayService(this);

            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;

            LoadData();
            Loaded += (_, _) => InitializeCategories();

            BackgroundColorPicker.SelectedColorChanged += (s, e) =>
                TankCanvas.Background = new SolidColorBrush(e.NewValue ?? Colors.Black);

            TextColorPicker.SelectedColorChanged += (s, e) =>
            {
                UpdateAllTaskColors();
            };

            DefaultFontSizeSlider.ValueChanged += (s, e) =>
            {
                foreach (var kv in _taskElements)
                {
                    var tb = kv.Key;
                    var task = kv.Value;
                    double offset = task.VarianceOffset ?? 0;
                    tb.FontSize = Math.Max(8, e.NewValue + offset);
                }
            };
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                if (_minimizingToTray) return;
                _minimizingToTray = true;

                ShowInTaskbar = false;
                Hide();
                _trayService.ShowTrayIcon();

                _minimizingToTray = false;
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isClosingConfirmed)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
            }
        }

        public void ConfirmAndExit()
        {
            _isClosingConfirmed = true;
            _trayService.Cleanup();
            System.Windows.Application.Current.Shutdown();
        }

        private void UpdateAllTaskColors()
        {
            var defaultBrush = new SolidColorBrush(TextColorPicker.SelectedColor ?? Colors.White);
            var priorityBrush = new SolidColorBrush(PriorityColorPicker.SelectedColor ?? Colors.Orange);
            foreach (var kv in _taskElements)
            {
                var tb = kv.Key;
                var item = kv.Value;
                tb.Foreground = item.IsPriority ? priorityBrush :
                                (item.IsCompleted ? Brushes.LimeGreen : defaultBrush);
                tb.Opacity = item.IsCompleted ? 0.65 : 0.92;
            }
        }

        private Color? ParseColor(string? argb)
        {
            if (string.IsNullOrWhiteSpace(argb)) return null;
            try { return (Color?)ColorConverter.ConvertFromString(argb); }
            catch { return null; }
        }

        private void LoadData()
        {
            var saveData = _dataService.Load();

            _categories.Clear();
            _categories.AddRange(saveData.Categories);

            _categories.RemoveAll(c => string.Equals(c, "General", StringComparison.OrdinalIgnoreCase));

            if (_categories.Contains("Default"))
                _categories.Remove("Default");
            _categories.Insert(0, "Default");

            CategoryCombo.ItemsSource = _categories;
            CategoryCombo.SelectedItem = _categories.Contains(_currentCategory) ? _currentCategory : "Default";

            TankCanvas.Children.Clear();
            _taskElements.Clear();

            foreach (var t in saveData.Tasks)
            {
                var task = new TodoItem
                {
                    Text = t.Text,
                    Category = string.Equals(t.Category, "General", StringComparison.OrdinalIgnoreCase) ? "Default" : (t.Category ?? "Default"),
                    IsCompleted = t.IsCompleted,
                    IsPriority = t.IsPriority,
                    VarianceOffset = t.VarianceOffset
                };

                var tb = CreateFloatingTaskTextBlock(task);
                TankCanvas.Children.Add(tb);
            }

            DefaultFontSizeSlider.Value = saveData.DefaultFontSize > 0 ? saveData.DefaultFontSize : 24;

            var textColor = ParseColor(saveData.TextColorArgb) ?? Colors.White;
            TextColorPicker.SelectedColor = textColor;

            var priorityColor = ParseColor(saveData.PriorityColorArgb) ?? Colors.Orange;
            PriorityColorPicker.SelectedColor = priorityColor;

            var bgColor = ParseColor(saveData.BackgroundColorArgb) ?? Color.FromArgb(255, 15, 15, 26);
            BackgroundColorPicker.SelectedColor = bgColor;
            TankCanvas.Background = new SolidColorBrush(bgColor);

            UpdateAllTaskColors();

            foreach (var kv in _taskElements)
            {
                var tb = kv.Key;
                var task = kv.Value;
                double offset = task.VarianceOffset ?? 0;
                tb.FontSize = Math.Max(8, DefaultFontSizeSlider.Value + offset);
            }

            RefreshTankVisibility(true);
        }

        private void SaveData()
        {
            var saveData = new SaveData
            {
                Categories = new List<string>(_categories),
                Tasks = new List<SavedTask>()
            };

            foreach (var task in _taskElements.Values)
            {
                saveData.Tasks.Add(new SavedTask
                {
                    Text = task.Text ?? "",
                    Category = task.Category ?? "Default",
                    IsCompleted = task.IsCompleted,
                    IsPriority = task.IsPriority,
                    VarianceOffset = task.VarianceOffset
                });
            }

            saveData.DefaultFontSize = DefaultFontSizeSlider.Value;
            saveData.TextColorArgb = TextColorPicker.SelectedColor?.ToString();
            saveData.PriorityColorArgb = PriorityColorPicker.SelectedColor?.ToString();
            saveData.BackgroundColorArgb = BackgroundColorPicker.SelectedColor?.ToString();

            _dataService.Save(saveData);
        }

        private TextBlock CreateFloatingTaskTextBlock(TodoItem task)
        {
            if (!task.VarianceOffset.HasValue)
            {
                task.VarianceOffset = _random.Next(-10, 25);
            }

            double finalSize = Math.Max(8, DefaultFontSizeSlider.Value + task.VarianceOffset.Value);

            var tb = new TextBlock
            {
                Text = task.Text,
                FontSize = finalSize,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Medium,
                Opacity = 0,
                Visibility = Visibility.Collapsed,
                DataContext = task,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 280
            };
            tb.MouseLeftButtonDown += TaskText_MouseLeftButtonDown;
            tb.MouseRightButtonDown += TaskText_MouseRightButtonDown;
            _taskElements[tb] = task;
            return tb;
        }

        private void RefreshTankVisibility(bool isInitialLoad = false)
        {
            const double fadeDurationSeconds = 0.3;

            foreach (var kv in _taskElements)
            {
                var tb = kv.Key;
                var item = kv.Value;
                bool shouldBeVisible = item.Category == _currentCategory;

                if (shouldBeVisible)
                {
                    if (tb.Visibility != Visibility.Visible)
                    {
                        tb.Visibility = Visibility.Visible;
                        tb.Opacity = 0;

                        if (isInitialLoad)
                        {
                            _animationService.StartDelayedRespawn(tb, TankCanvas);
                        }
                        else
                        {
                            _animationService.StartFloating(tb, TankCanvas, isNewTask: true);
                            _animationService.FadeInTask(tb);
                        }
                    }
                }
                else
                {
                    if (tb.Visibility == Visibility.Visible)
                    {
                        var fadeOut = new DoubleAnimation(tb.Opacity, 0, TimeSpan.FromSeconds(fadeDurationSeconds));
                        fadeOut.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn };
                        fadeOut.Completed += (_, __) =>
                        {
                            tb.Visibility = Visibility.Collapsed;
                            tb.BeginAnimation(OpacityProperty, null);
                        };
                        tb.BeginAnimation(OpacityProperty, fadeOut);
                    }
                }
            }
        }

        private void TaskText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBlock tb || tb.DataContext is not TodoItem item) return;
            e.Handled = true;
            item.IsPriority = !item.IsPriority;
            UpdateAllTaskColors();
        }

        private void TaskText_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBlock tb) return;
            e.Handled = true;
            if (tb.Tag is Storyboard story) story.Stop();
            var fade = new DoubleAnimation(tb.Opacity, 0, TimeSpan.FromSeconds(0.4));
            var shrink = new DoubleAnimation(1.0, 0, TimeSpan.FromSeconds(0.4));
            var storyOut = new Storyboard();
            Storyboard.SetTarget(fade, tb);
            Storyboard.SetTargetProperty(fade, new PropertyPath(FrameworkElement.OpacityProperty));
            storyOut.Children.Add(fade);
            var scale = new ScaleTransform();
            tb.RenderTransform = scale;
            tb.RenderTransformOrigin = new Point(0.5, 0.5);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
            storyOut.Completed += (_, _) =>
            {
                TankCanvas.Children.Remove(tb);
                _taskElements.Remove(tb);
            };
            storyOut.Begin(tb);
        }

        private void AddTask_Click(object sender, RoutedEventArgs e) => AddNewTask();

        private void NewTaskText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddNewTask();
                e.Handled = true;
            }
        }

        private void AddNewTask()
        {
            string text = NewTaskText.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            var task = new TodoItem
            {
                Text = text,
                Category = _currentCategory,
                IsCompleted = false
            };

            var tb = CreateFloatingTaskTextBlock(task);
            TankCanvas.Children.Add(tb);

            // Prep for spawn-in (mimics the 'shouldBeVisible' branch in RefreshTankVisibility)
            tb.Visibility = Visibility.Visible;
            tb.Opacity = 0;

            _animationService.StartFloating(tb, TankCanvas, isNewTask: true);
            _animationService.FadeInTask(tb);

            UpdateAllTaskColors();

            NewTaskText.Text = "";
            NewTaskText.Focus();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
            _isClosingConfirmed = true;
            _trayService?.Cleanup();
            System.Windows.Application.Current.Shutdown();
        }

        private void WindowControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button btn) btn.Foreground = Brushes.White;
        }

        private void WindowControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn) btn.Foreground = Brushes.LightGray;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void PriorityColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            UpdateAllTaskColors();
        }

        private void InitializeCategories()
        {
            CategoryCombo.ItemsSource = _categories;
            CategoryCombo.SelectedItem = _currentCategory;
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryCombo.SelectedItem is string cat)
            {
                _currentCategory = cat;
                RefreshTankVisibility();
            }
        }

        private void NewCategory_Click(object sender, RoutedEventArgs e)
        {
            string input = Interaction.InputBox("Enter new category name:", "New Category", "");

            if (string.IsNullOrWhiteSpace(input))
                return;

            string newCategory = input.Trim();

            if (string.IsNullOrEmpty(newCategory))
            {
                MessageBox.Show("Category name cannot be empty.", "Invalid Name");
                return;
            }

            if (_categories.Contains(newCategory, StringComparer.OrdinalIgnoreCase))
            {
                MessageBox.Show("A category with this name already exists.", "Duplicate");
                return;
            }

            _categories.Add(newCategory);
            _currentCategory = newCategory;
            CategoryCombo.SelectedItem = newCategory;
            RefreshTankVisibility();
            SaveData();
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryCombo.SelectedItem is not string selected || selected == "Default")
            {
                MessageBox.Show("Cannot delete the Default category.", "Protected");
                return;
            }

            bool hasTasks = false;
            foreach (var task in _taskElements.Values)
            {
                if (string.Equals(task.Category, selected, StringComparison.OrdinalIgnoreCase))
                {
                    hasTasks = true;
                    break;
                }
            }

            if (hasTasks)
            {
                var result = MessageBox.Show($"Category '{selected}' contains tasks. Delete category and move tasks to Default?",
                                           "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;

                foreach (var task in _taskElements.Values)
                {
                    if (string.Equals(task.Category, selected, StringComparison.OrdinalIgnoreCase))
                        task.Category = "Default";
                }
            }

            _categories.Remove(selected);
            _currentCategory = "Default";
            CategoryCombo.SelectedItem = "Default";
            RefreshTankVisibility();
            SaveData();
        }
    }
}