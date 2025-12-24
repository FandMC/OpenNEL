namespace OpenNEL.GameLauncher.Utils.Progress;

public static class SyncProgressBarUtil
{
    public class ProgressBarOptions
    {
        public int Width { get; set; } = 45;
        public char FillChar { get; set; } = '■';
        public char EmptyChar { get; set; } = '·';
        public string ProgressFormat { get; set; } = "{0:P1}";
        public bool ShowPercentage { get; set; } = true;
        public bool ShowElapsedTime { get; set; } = true;
        public bool ShowEta { get; set; } = true;
        public bool ShowSpinner { get; set; } = true;
        public ConsoleColor FillColor { get; set; } = ConsoleColor.Cyan;
        public ConsoleColor EmptyColor { get; set; } = ConsoleColor.DarkGray;
        public ConsoleColor SpinnerColor { get; set; } = ConsoleColor.Cyan;
        public string Prefix { get; set; } = "";
        public string Suffix { get; set; } = "";
        public bool LastLineNewline { get; set; } = true;
    }

    public class ProgressBar : IDisposable
    {
        private readonly ProgressBarOptions _options;
        private readonly DateTime _startTime;
        private readonly char[] _spinnerChars;
        private int _current;
        private int _spinnerIndex;
        private bool _disposed;

        public ProgressBar(int total, ProgressBarOptions? options = null)
        {
            _options = options ?? new ProgressBarOptions();
            _startTime = DateTime.Now;
            _spinnerChars = ['|', '/', '─', '\\'];
        }

        public void Update(int current, string action)
        {
            if (!_disposed)
            {
                _current = current;
                _spinnerIndex = (_spinnerIndex + 1) % _spinnerChars.Length;
                Display(action);
            }
        }

        private void Display(string action)
        {
            // GUI环境中不显示控制台进度条
        }

        public static void ClearCurrent()
        {
            // GUI环境中不需要清除
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    public class ProgressReport
    {
        public int Percent { get; set; }
        public string Message { get; set; } = "";
    }

    private static readonly Lock Lock = new();

    private static string GetAnsiColorCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\u001b[30m",
            ConsoleColor.DarkRed => "\u001b[31m",
            ConsoleColor.DarkGreen => "\u001b[32m",
            ConsoleColor.DarkYellow => "\u001b[33m",
            ConsoleColor.DarkBlue => "\u001b[34m",
            ConsoleColor.DarkMagenta => "\u001b[35m",
            ConsoleColor.DarkCyan => "\u001b[36m",
            ConsoleColor.Gray => "\u001b[37m",
            ConsoleColor.DarkGray => "\u001b[90m",
            ConsoleColor.Red => "\u001b[91m",
            ConsoleColor.Green => "\u001b[92m",
            ConsoleColor.Yellow => "\u001b[93m",
            ConsoleColor.Blue => "\u001b[94m",
            ConsoleColor.Magenta => "\u001b[95m",
            ConsoleColor.Cyan => "\u001b[96m",
            ConsoleColor.White => "\u001b[97m",
            _ => "\u001b[37m",
        };
    }
}
