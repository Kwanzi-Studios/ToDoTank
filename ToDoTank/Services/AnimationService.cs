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
            // Stop any existing animation if needed (though now tag is velocity)
            if (tb.Tag is Storyboard oldStory) oldStory.Stop();

            double canvasW = canvas.ActualWidth;
            double canvasH = canvas.ActualHeight;
            if (canvasW <= 0 || canvasH <= 0) return; // Canvas not sized yet; skip or retry if needed

            // Random direction using angle for uniform distribution
            double angle = _random.NextDouble() * 2 * Math.PI;
            double dx = Math.Cos(angle);
            double dy = Math.Sin(angle);

            // Speed in px/s (reduced range for smoother feel; adjust as needed)
            double speed = 50 + _random.NextDouble() * 50;

            // Store velocity (vx, vy)
            double vx = dx * speed;
            double vy = dy * speed;
            tb.Tag = new double[] { vx, vy };

            // Set initial position
            double x, y;
            if (isNewTask)
            {
                // Start around center with random offset
                double centerX = canvasW / 2;
                double centerY = canvasH / 2;
                // Use pseudo-Gaussian for clustering around center (Box-Muller approximation)
                double u1 = _random.NextDouble();
                double u2 = _random.NextDouble();
                double offsetX = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2) * 100; // Std dev 100px
                double offsetY = Math.Sqrt(-2 * Math.Log(u1)) * Math.Sin(2 * Math.PI * u2) * 100;
                x = centerX + offsetX;
                y = centerY + offsetY;
                // Clamp to ensure within bounds initially, minus some margin
                double margin = 50;
                x = Math.Max(margin, Math.Min(x, canvasW - margin));
                y = Math.Max(margin, Math.Min(y, canvasH - margin));
            }
            else
            {
                // For non-new (though now unified, kept for reference)
                double off = 400;
                x = dx > 0 ? -off : canvasW + off;
                y = dy > 0 ? -off : canvasH + off;
            }
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);

            // Add to active list if not already
            if (!_activeTasks.Contains(tb))
            {
                _activeTasks.Add(tb);
            }

            // Start the global update loop if not running
            if (_activeTasks.Count == 1)
            {
                CompositionTarget.Rendering += UpdatePositions;
            }
        }

        private void UpdatePositions(object sender, EventArgs e)
        {
            if (e is not RenderingEventArgs renderingArgs) return;

            double currentTime = renderingArgs.RenderingTime.TotalSeconds;
            double deltaTime = currentTime - _lastTime;
            _lastTime = currentTime;
            if (deltaTime <= 0) return; // Skip if no time passed

            for (int i = _activeTasks.Count - 1; i >= 0; i--)
            {
                var tb = _activeTasks[i];
                if (tb.Parent is not Canvas canvas || !canvas.Children.Contains(tb))
                {
                    // Task removed (e.g., deleted); clean up
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

                // Update position
                left += vx * deltaTime;
                top += vy * deltaTime;

                // Get size (measure if needed)
                if (tb.ActualWidth <= 0 || tb.ActualHeight <= 0)
                {
                    tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                }
                double tbW = tb.ActualWidth;
                double tbH = tb.ActualHeight;

                // Wrap around when fully off-screen
                if (left > w)
                {
                    left = -tbW; // Enter from left
                }
                else if (left + tbW < 0)
                {
                    left = w; // Enter from right
                }

                if (top > h)
                {
                    top = -tbH; // Enter from top
                }
                else if (top + tbH < 0)
                {
                    top = h; // Enter from bottom
                }

                Canvas.SetLeft(tb, left);
                Canvas.SetTop(tb, top);
            }

            // Stop loop if no tasks left
            if (_activeTasks.Count == 0)
            {
                CompositionTarget.Rendering -= UpdatePositions;
            }
        }

        public void StartDelayedRespawn(TextBlock tb, Canvas canvas)
        {
            double delaySec = 0.3 + _random.NextDouble() * 1.8; // 0.3–2.1 seconds
            var delayTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(delaySec)
            };
            delayTimer.Tick += (_, _) =>
            {
                delayTimer.Stop();
                // Set position and start animation after delay
                StartFloating(tb, canvas, isNewTask: true); // Use isNewTask=true for centered start
                FadeInTask(tb); // Fade in after positioning
            };
            delayTimer.Start();
        }

        public void FadeInTask(TextBlock tb)
        {
            if (tb.DataContext is not TodoItem item) return;
            double targetOpacity = item.IsCompleted ? 0.65 : 0.92;
            tb.Opacity = 0; // Ensure starting from hidden
            var fade = new DoubleAnimation(0, targetOpacity, TimeSpan.FromSeconds(0.5));
            var story = new Storyboard();
            Storyboard.SetTarget(fade, tb);
            Storyboard.SetTargetProperty(fade, new PropertyPath(FrameworkElement.OpacityProperty));
            story.Children.Add(fade);
            story.Begin(tb);
        }
    }
}