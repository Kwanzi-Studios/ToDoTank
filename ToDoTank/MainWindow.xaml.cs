using System;
using System.Collections.Generic;
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
        private readonly List<string> _categories = new() { "General" };
        private string _currentCategory = "General";
        private readonly Dictionary<TextBlock, TodoItem> _taskElements = new();
        private readonly DataService _dataService = new DataService();
        private readonly AnimationService _animationService = new AnimationService();
        private readonly TrayService _trayService;

        public MainWindow()
        {
            InitializeComponent();
            _trayService = new TrayService(this);
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
                foreach (var tb in _taskElements.Keys)
                    tb.FontSize = e.NewValue;
            };
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

        private void LoadData()
        {
            var (categories, tasks) = _dataService.Load();
            _categories.Clear();
            _categories.AddRange(categories);
            CategoryCombo.ItemsSource = _categories;
            CategoryCombo.SelectedItem = _categories.Contains(_currentCategory)
                ? _currentCategory
                : "General";
            TankCanvas.Children.Clear();
            _taskElements.Clear();
            foreach (var task in tasks)
            {
                var tb = CreateFloatingTaskTextBlock(task);
                TankCanvas.Children.Add(tb);
                _animationService.StartDelayedRespawn(tb, TankCanvas);
            }
            UpdateAllTaskColors();
            foreach (var tb in _taskElements.Keys)
            {
                tb.Opacity = 0; // Hide initially after setting target opacity in UpdateAllTaskColors
            }
        }

        private void SaveData()
        {
            var tasks = new List<TodoItem>(_taskElements.Values);
            _dataService.Save(_categories, tasks);
        }

        private TextBlock CreateFloatingTaskTextBlock(TodoItem task)
        {
            var tb = new TextBlock
            {
                Text = task.Text,
                FontSize = 18 + _random.Next(-4, 9),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Medium,
                Opacity = 0.92, // Default target opacity
                DataContext = task,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 280
            };
            tb.MouseLeftButtonDown += TaskText_MouseLeftButtonDown;
            tb.MouseRightButtonDown += TaskText_MouseRightButtonDown;
            _taskElements[tb] = task;
            return tb;
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
            _animationService.StartFloating(tb, TankCanvas, isNewTask: true);
            UpdateAllTaskColors();
            tb.Opacity = 0; // Hide initially after setting target opacity
            _animationService.FadeInTask(tb);
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

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveData();
            _trayService?.Cleanup();
            base.OnClosed(e);
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
                _currentCategory = cat;
        }

        private void NewCategory_Click(object sender, RoutedEventArgs e) { }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e) { }
    }
}