using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DayCountdown
{
    public partial class CountdownWindow : Window
    {
        public const double DefaultWidth = 320;
        public const double DefaultHeight = 180;

        /// <summary>所有存活的倒计时窗口（用于右上角整理避让）</summary>
        public static readonly List<CountdownWindow> All = new();

        private readonly CountdownItem _item;
        private readonly AppConfig _cfg;
        private readonly Action _onChanged;
        private readonly DispatcherTimer _saveTimer;

        /// <summary>此窗口对应的数据项</summary>
        public CountdownItem Item => _item;

        public CountdownWindow(CountdownItem item, AppConfig cfg, Action onChanged)
        {
            InitializeComponent();
            _item = item;
            _cfg = cfg;
            _onChanged = onChanged;
            All.Add(this);

            Width = item.Width;
            Height = item.Height;
            Topmost = item.Topmost;
            TopmostMenuItem.IsChecked = item.Topmost;
            UpdatePinVisual();

            if (item.X.HasValue && item.Y.HasValue)
            {
                Left = item.X.Value;
                Top = item.Y.Value;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // 几何变化延迟保存，避免频繁写盘
            _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); _onChanged(); };
            SizeChanged += (_, _) => { _item.Width = Width; _item.Height = Height; RestartSaveTimer(); };

            ApplyCardOpacity();
            RefreshDisplay();
        }

        // ---------- 显示 ----------

        /// <summary>按全局设置应用卡片背景透明度</summary>
        private void ApplyCardOpacity()
        {
            double a = Math.Clamp(_cfg.CardOpacity, 0.05, 1.0);
            CardBorder.Background = new SolidColorBrush(
                Color.FromArgb((byte)Math.Round(a * 255), 0x1A, 0x1A, 0x30));
        }

        /// <summary>全局背景透明度变化时刷新所有存活窗口（设置中拖动滑块实时预览）</summary>
        public static void ApplyOpacityToAll(AppConfig cfg)
        {
            foreach (var w in All)
                w.ApplyCardOpacity();
        }

        private void RefreshDisplay()
        {
            var today = DateTime.Today;
            int days = (_item.TargetDate - today).Days;

            NameText.Text = _item.Name;

            if (days > 0)
            {
                DayNumberText.Text = days.ToString();
                DayUnitText.Text = "天";
                DayUnitText.Visibility = Visibility.Visible;
                StatusText.Text = days == 1 ? "明天就是目标日" : $"距目标还有 {days} 天";
            }
            else if (days == 0)
            {
                DayNumberText.Text = "0";
                DayUnitText.Text = "";
                DayUnitText.Visibility = Visibility.Collapsed;
                StatusText.Text = "就是今天！";
            }
            else
            {
                DayNumberText.Text = (-days).ToString();
                DayUnitText.Text = "天前";
                DayUnitText.Visibility = Visibility.Visible;
                StatusText.Text = "目标日期已过";
            }

            TargetInfoText.Text = $"{_item.TargetDate:yyyy-MM-dd} {WeekdayCn(_item.TargetDate)}";
        }

        private static string WeekdayCn(DateTime d) =>
            new[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" }[(int)d.DayOfWeek];

        // ---------- 移动 / 8 方向缩放 ----------

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            // 双击空白区域恢复默认大小（标题栏文字上的双击是重命名，跳过）
            if (e.ClickCount == 2)
            {
                if (e.OriginalSource is not TextBlock { Name: "NameText" })
                    RestoreDefaultSize();
                e.Handled = true;
                return;
            }

            DragMove();
        }

        private void RestoreDefaultSize()
        {
            Width = DefaultWidth;
            Height = DefaultHeight;
            _item.Width = Width;
            _item.Height = Height;
            _onChanged();
        }

        private void NameText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                Rename();
        }

        private void ResizeDragDelta(object sender, DragDeltaEventArgs e)
        {
            double h = e.HorizontalChange, v = e.VerticalChange;
            switch ((string)((Thumb)sender).Tag)
            {
                case "L": ResizeLeft(h); break;
                case "R": Width = Math.Max(MinWidth, Width + h); break;
                case "T": ResizeTop(v); break;
                case "B": Height = Math.Max(MinHeight, Height + v); break;
                case "TL": ResizeLeft(h); ResizeTop(v); break;
                case "TR": Width = Math.Max(MinWidth, Width + h); ResizeTop(v); break;
                case "BL": ResizeLeft(h); Height = Math.Max(MinHeight, Height + v); break;
                case "BR": Width = Math.Max(MinWidth, Width + h); Height = Math.Max(MinHeight, Height + v); break;
            }
            // 拖动过程中持续记录几何，防中断丢失
            _item.X = Left; _item.Y = Top;
            _item.Width = Width; _item.Height = Height;
            RestartSaveTimer();
        }

        private void ResizeLeft(double h)
        {
            double newWidth = Math.Max(MinWidth, Width - h);
            Left += Width - newWidth;
            Width = newWidth;
        }

        private void ResizeTop(double v)
        {
            double newHeight = Math.Max(MinHeight, Height - v);
            Top += Height - newHeight;
            Height = newHeight;
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            _item.X = Left;
            _item.Y = Top;
            RestartSaveTimer();
        }

        private void RestartSaveTimer()
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        // ---------- 自治功能 ----------

        private void New_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new NewCountdownDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                var item = new CountdownItem
                {
                    Name = dlg.ResultName.Length > 0 ? dlg.ResultName : "倒计时",
                    TargetDate = dlg.ResultDate
                };
                _cfg.Items.Add(item);
                _onChanged();
                var win = new CountdownWindow(item, _cfg, _onChanged);
                win.Show();
            }
        }

        private void Rename_Click(object sender, RoutedEventArgs e) => Rename();

        private void Rename()
        {
            var name = Dialogs.Prompt(this, "重命名备注", _item.Name);
            if (name != null && name.Length > 0 && name != _item.Name)
            {
                _item.Name = name;
                RefreshDisplay();
                _onChanged();
            }
        }

        private void PickDate_Click(object sender, RoutedEventArgs e) => PickDate();

        private void PickDate()
        {
            var date = Dialogs.PickDate(this, "设置目标日期", _item.TargetDate);
            if (date.HasValue && date.Value != _item.TargetDate)
            {
                _item.TargetDate = date.Value;
                RefreshDisplay();
                _onChanged();
            }
        }

        /// <summary>双击底部日期区域直接修改目标日期</summary>
        private void TargetInfoText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                PickDate();
                e.Handled = true; // 阻止冒泡到 Root（避免触发恢复默认大小）
            }
        }

        private void ArrangeTopRight_Click(object sender, RoutedEventArgs e)
        {
            WindowArranger.MoveToTopRight(this, All);
            _onChanged();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow(_cfg, _onChanged) { Owner = this };
            win.ShowDialog();
        }

        private void TopmostMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            SetPin(true);
        }

        private void TopmostMenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            SetPin(false);
        }

        private void PinToggle_Click(object sender, RoutedEventArgs e)
        {
            SetPin(!Topmost);
        }

        private void SetPin(bool on)
        {
            Topmost = on;
            _item.Topmost = on;
            TopmostMenuItem.IsChecked = on;
            UpdatePinVisual();
            _onChanged();
        }

        private void UpdatePinVisual()
        {
            if (Topmost)
            {
                PinButton.Foreground = Brushes.White;
                PinButton.Background = new SolidColorBrush(Color.FromArgb(0x38, 0x6C, 0x5C, 0xE7));
            }
            else
            {
                PinButton.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xA8, 0xA8, 0xC8));
                PinButton.Background = Brushes.Transparent;
            }
        }

        /// <summary>关闭窗口但保留记录（可在设置中重新打开）</summary>
        private void CloseHide_Click(object sender, RoutedEventArgs e)
        {
            if (All.Count == 1)
            {
                // 最后一个可见窗口：关闭即退出应用，风格一致的退出提醒
                var ok = Dialogs.Confirm(this, "退出 DayCountdown",
                    "这是最后一个可见窗口，关闭后将退出应用。\n所有倒计时记录都会保留，重新打开应用即可恢复。",
                    "退出", "取消");
                if (!ok)
                    return;
            }
            _item.Hidden = true;
            _onChanged();
            Close();
        }

        /// <summary>真正删除此倒计时（连同记录）</summary>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var ok = Dialogs.Confirm(this, "删除倒计时",
                $"确定要删除倒计时「{_item.Name}」吗？此操作不可恢复。",
                "删除", "取消");
            if (!ok)
                return;

            _cfg.Items.RemoveAll(i => i.Id == _item.Id);
            _onChanged();
            Close();
        }

        private void Root_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            TopmostMenuItem.IsChecked = Topmost;
        }

        protected override void OnClosed(EventArgs e)
        {
            All.Remove(this);
            base.OnClosed(e);
        }
    }
}
