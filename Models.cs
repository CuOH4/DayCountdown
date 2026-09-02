using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;

namespace DayCountdown
{
    /// <summary>单个倒计时条目的数据</summary>
    public class CountdownItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "倒计时";
        public DateTime TargetDate { get; set; } = DateTime.Today.AddDays(7);
        public double? X { get; set; } // null = 尚未定位，首次显示时居中
        public double? Y { get; set; }
        public double Width { get; set; } = CountdownWindow.DefaultWidth;
        public double Height { get; set; } = CountdownWindow.DefaultHeight;
        public bool Topmost { get; set; }
        public bool Hidden { get; set; } // 关闭(隐藏)但保留记录，可在设置中重新打开
    }

    public class AppConfig
    {
        public List<CountdownItem> Items { get; set; } = new();
        public bool AutoStart { get; set; }
        /// <summary>倒计时卡片背景不透明度（0.05~1，1 = 完全不透明）</summary>
        public double CardOpacity { get; set; } = 0.94;
    }

    /// <summary>配置读写（%AppData%\DayCountdown\config.json），兼容旧版单倒计时配置</summary>
    public static class ConfigStore
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DayCountdown");
        public static readonly string FilePath = System.IO.Path.Combine(Dir, "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new AppConfig();

                string json = File.ReadAllText(FilePath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                if (cfg != null && cfg.Items.Count > 0)
                    return cfg;

                // 旧版单目标配置迁移
                var old = JsonSerializer.Deserialize<OldConfig>(json);
                if (old != null && DateTime.TryParse(old.TargetDate, out var dt))
                {
                    cfg = new AppConfig { AutoStart = old.AutoStart };
                    cfg.Items.Add(new CountdownItem { Name = "目标", TargetDate = dt, Topmost = old.Topmost });
                    return cfg;
                }
            }
            catch
            {
                // 配置损坏时使用默认
            }
            return new AppConfig();
        }

        public static void Save(AppConfig cfg)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(FilePath,
                    JsonSerializer.Serialize(cfg, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 中文直接可读
                    }));
            }
            catch
            {
                // 写失败不影响使用
            }
        }

        private class OldConfig
        {
            public string TargetDate { get; set; } = "";
            public bool Topmost { get; set; }
            public bool AutoStart { get; set; }
        }
    }

    /// <summary>开机自启（注册表 HKCU Run 键）</summary>
    public static class AutoStartHelper
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "DayCountdown";

        public static void Set(bool enable)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKeyPath);
                if (enable)
                    key.SetValue(RunValueName, "\"" + Environment.ProcessPath + "\"");
                else
                    key.DeleteValue(RunValueName, false);
            }
            catch
            {
                // 注册表被策略限制时静默失败
            }
        }
    }

    /// <summary>一键整理：将窗口移至屏幕右上角，自动避让已有窗口向下排布</summary>
    public static class WindowArranger
    {
        private const double Gap = 12;

        public static void MoveToTopRight(Window w, IEnumerable<Window> others)
        {
            var wa = SystemParameters.WorkArea;
            double x = wa.Right - w.Width - Gap;
            double y = wa.Top + Gap;

            // 从右上角顶部开始，若与某窗口重叠则跳到其正下方，循环直到找到空位
            for (int i = 0; i < 64; i++)
            {
                bool blocked = false;
                foreach (var o in others)
                {
                    if (o == w || !o.IsVisible)
                        continue;
                    if (Overlaps(w, x, y, o))
                    {
                        y = Math.Max(y, o.Top + o.Height + Gap);
                        blocked = true;
                    }
                }
                if (!blocked)
                    break;
            }

            w.Left = x;
            w.Top = y;
        }

        /// <summary>两矩形是否重叠（各收缩 4px 判断，避免边缘缩放区误判）</summary>
        private static bool Overlaps(Window self, double x, double y, Window other)
        {
            const double inset = 4;
            double l1 = x + inset, t1 = y + inset, r1 = x + self.Width - inset, b1 = y + self.Height - inset;
            double l2 = other.Left + inset, t2 = other.Top + inset, r2 = other.Left + other.Width - inset, b2 = other.Top + other.Height - inset;
            return l1 < r2 && r1 > l2 && t1 < b2 && b1 > t2;
        }
    }
}
