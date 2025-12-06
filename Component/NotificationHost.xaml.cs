using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.ObjectModel;
using System.Numerics;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Composition;

namespace OpenNEL_WinUI
{
    public sealed partial class NotificationHost : UserControl
    {
        public ObservableCollection<ToastItem> Items { get; } = new ObservableCollection<ToastItem>();
        public static NotificationHost Instance { get; private set; }

        public NotificationHost()
        {
            this.InitializeComponent();
            this.Loaded += NotificationHost_Loaded;
        }

        private void NotificationHost_Loaded(object sender, RoutedEventArgs e)
        {
            Instance = this;
        }

        public static void ShowGlobal(string text, ToastLevel level)
        {
            var inst = Instance;
            if (inst == null || string.IsNullOrWhiteSpace(text)) return;
            var colors = GetColors(level);
            inst.Items.Add(new ToastItem
            {
                Text = text,
                Background = new SolidColorBrush(colors.bg),
                Foreground = new SolidColorBrush(colors.fg),
                LifetimeMs = 3000,
                Level = level
            });
        }

        static (Color bg, Color fg) GetColors(ToastLevel level)
        {
            if (level == ToastLevel.Success) return (Color.FromArgb(255, 34, 197, 94), Colors.White);
            if (level == ToastLevel.Warning) return (Color.FromArgb(255, 245, 158, 11), Colors.Black);
            if (level == ToastLevel.Error) return (Color.FromArgb(255, 239, 68, 68), Colors.White);
            return (Color.FromArgb(255, 31, 31, 31), Colors.White);
        }

        private void Toast_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                AnimateY(fe, -40, 0, 200);
                var item = fe.DataContext as ToastItem;
                if (item == null) return;
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(item.LifetimeMs) };
                timer.Tick += (s, ev) =>
                {
                    timer.Stop();
                    AnimateY(fe, 0, -40, 200, () =>
                    {
                        Items.Remove(item);
                    });
                };
                timer.Start();
            }
        }

        void AnimateY(FrameworkElement element, double from, double to, int durationMs, Action completed = null)
        {
            var sb = new Storyboard();
            var da = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(da, element);
            Storyboard.SetTargetProperty(da, "(UIElement.RenderTransform).(TranslateTransform.Y)");
            sb.Children.Add(da);
            if (completed != null)
            {
                sb.Completed += (s, e) => completed();
            }
            sb.Begin();
        }
    }

    public enum ToastLevel
    {
        Normal,
        Success,
        Warning,
        Error
    }

    public class ToastItem
    {
        public string Text { get; set; }
        public Brush Background { get; set; }
        public Brush Foreground { get; set; }
        public int LifetimeMs { get; set; }
        public ToastLevel Level { get; set; }
    }
}
