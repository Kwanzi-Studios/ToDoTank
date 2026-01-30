using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ToDoTank.Models;

namespace ToDoTank.Services
{
    public class AnimationService
    {
        private readonly Random _random = new Random();
        private readonly List<TextBlock> _activeTasks = new List<TextBlock>();
        private double _lastTime = 0;

        public void StartFloating(TextBlock tb, Canvas canvas, bool isNewTask = true)
        {
            // Stop any existing animation
            if (tb.Tag is Storyboard oldStory) oldStory.Stop();

            double canvasW = canvas.ActualWidth;
            double canvasH = canvas.ActualHeight;
            if (canvasW <= 0 || canvasH <= 0) return;

            // Random direction
            double angle = _random.NextDouble() * 2 * Math.PI;
            double dx = Math.Cos(angle);
            double dy = Math.Sin(angle);

            // Speed in px/s
            double speed = 50 + _random.NextDouble() * 50;

            // Store velocity (vx, vy)
            double vx = dx * speed;
            double vy = dy * speed;

            // NEW: Start with zero velocity for initial calm period
            tb.Tag = new double[] { 0, 0 };  // zero initially

            // Schedule velocity activation after short delay (prevents instant exit)
            var delayTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.6 + _random.NextDouble() * 0.6)  // 0.6–1.2s delay
            };
            delayTimer.Tick += (_, _) =>
            {
                delayTimer.Stop();
                tb.Tag = new double[] { vx, vy };  // now apply full velocity
            };
            delayTimer.Start();

            // Set initial position (centered with offset)
            double centerX = canvasW / 2;
            double centerY = canvasH / 2;
            double u1 = _random.NextDouble();
            double u2 = _random.NextDouble();
            double offsetX = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2) * 100;
            double offsetY = Math.Sqrt(-2 * Math.Log(u1)) * Math.Sin(2 * Math.PI * u2) * 100;
            double x = centerX + offsetX;
            double y = centerY + offsetY;

            double margin = 50;
            x = Math.Max(margin, Math.Min(x, canvasW - margin));
            y = Math.Max(margin, Math.Min(y, canvasH - margin));

            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);

            if (!_activeTasks.Contains(tb))
            {
                _activeTasks.Add(tb);
            }

            if (_activeTasks.Count == 1)
            {
                CompositionTarget.Rendering += UpdatePositions;
            }
        }

        private void UpdatePositions(object? sender, EventArgs e)
        {
            if (e is not RenderingEventArgs renderingArgs) return;

            double currentTime = renderingArgs.RenderingTime.TotalSeconds;
            double deltaTime = currentTime - _lastTime;
            _lastTime = currentTime;
            if (deltaTime <= 0) return;

            for (int i = _activeTasks.Count - 1; i >= 0; i--)
            {
                var tb = _activeTasks[i];
                if (tb.Parent is not Canvas canvas || !canvas.Children.Contains(tb))
                {
                    _activeTasks.RemoveAt(i);
                    continue;
                }

                double w = canvas.ActualWidth;
                double h = canvas.ActualHeight;
                double left = Canvas.GetLeft(tb);
                double top = Canvas.GetTop(tb);

                if (tb.Tag is not double[] vel) continue;
                double vx = vel[0];
                double vy = vel[1];

                left += vx * deltaTime;
                top += vy * deltaTime;

                if (tb.ActualWidth <= 0 || tb.ActualHeight <= 0)
                {
                    tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                }
                double tbW = tb.ActualWidth;
                double tbH = tb.ActualHeight;

                // Wrap-around (your original logic, unchanged)
                if (left > w)
                {
                    left = -tbW;
                }
                else if (left + tbW < 0)
                {
                    left = w;
                }
                if (top > h)
                {
                    top = -tbH;
                }
                else if (top + tbH < 0)
                {
                    top = h;
                }

                Canvas.SetLeft(tb, left);
                Canvas.SetTop(tb, top);
            }

            if (_activeTasks.Count == 0)
            {
                CompositionTarget.Rendering -= UpdatePositions;
            }
        }

        public void StartDelayedRespawn(TextBlock tb, Canvas canvas)
        {
            double delaySec = 0.3 + _random.NextDouble() * 1.8;
            var delayTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(delaySec)
            };
            delayTimer.Tick += (_, _) =>
            {
                delayTimer.Stop();
                StartFloating(tb, canvas, isNewTask: true);
                FadeInTask(tb);
            };
            delayTimer.Start();
        }

        public void FadeInTask(TextBlock tb)
        {
            if (tb.DataContext is not TodoItem item) return;
            double targetOpacity = item.IsCompleted ? 0.65 : 0.92;
            tb.Opacity = 0;
            var fade = new DoubleAnimation(0, targetOpacity, TimeSpan.FromSeconds(0.5));
            var story = new Storyboard();
            Storyboard.SetTarget(fade, tb);
            Storyboard.SetTargetProperty(fade, new PropertyPath(FrameworkElement.OpacityProperty));
            story.Children.Add(fade);
            story.Begin(tb);
        }
    }
}