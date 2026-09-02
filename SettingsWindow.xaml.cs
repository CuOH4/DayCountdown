using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DayCountdown
{
    public partial class SettingsWindow : Window
    {
        private readonly AppConfig _cfg;
        private readonly Action _onChanged;
        private readonly DispatcherTimer _opacitySaveTimer;
        private bool _loading = true;

        public sealed class ItemVm
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string StateText { get; set; } = "";
            public Brush StateColor { get; set; } = Brushes.Gray;
            public string DateText { get; set; } = "";
        }

        public SettingsWindow(AppConfig cfg, Action onChanged)
        {
            InitializeComponent();
            _cfg = cfg;
            _onChanged = onChanged;
            AutoStartToggle.IsChecked = cfg.AutoStart;

            // 拖动滑块时频繁触发，用防抖延迟写盘
            _opacitySaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _opacitySaveTimer.Tick += (_, _) => { _opacitySaveTimer.Stop(); _onChanged(); };

            _loading = true;
            OpacitySlider.Value = Math.Clamp(cfg.CardOpacity * 100, OpacitySlider.Minimum, OpacitySlider.Maximum);
            OpacityValueText.Text = $"{OpacitySlider.Value:0}%";
            _loading = false;

            RefreshItems();
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading)
                return;
            _cfg.CardOpacity = OpacitySlider.Value / 100.0;
            OpacityValueText.Text = $"{OpacitySlider.Value:0}%";
            CountdownWindow.ApplyOpacityToAll(_cfg); // 实时预览
            _opacitySaveTimer.Stop();
            _opacitySaveTimer.Start();
        }

        private void RefreshItems()
        {
            var today = DateTime.Today;
            var vms = _cfg.Items
                .OrderBy(i => i.Hidden)
                .ThenBy(i => i.TargetDate)
                .Select(i =>
                {
                    bool open = CountdownWindow.All.Any(w => w.Item.Id == i.Id);
                    int d = (i.TargetDate - today).Days;
                    string days = d switch
                    {
                        > 0 => $"剩 {d} 天",
                        0 => "今天",
                        _ => $"已过 {-d} 天"
                    };
                    return new ItemVm
                    {
                        Id = i.Id,
                        Name = i.Name,
                        StateText = open ? "运行中" : "已关闭",
                        StateColor = open
                            ? new SolidColorBrush(Color.FromRgb(0x00, 0xCE, 0xC9))
                            : new SolidColorBrush(Color.FromRgb(0x8E, 0x8E, 0xB2)),
                        DateText = $"{i.TargetDate:yyyy-MM-dd} · {days}"
                    };
                })
                .ToList();
            ItemsList.ItemsSource = vms;
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not string id)
                return;
            var item = _cfg.Items.FirstOrDefault(i => i.Id == id);
            if (item == null)
                return;

            var win = CountdownWindow.All.FirstOrDefault(w => w.Item.Id == id);
            if (win != null)
            {
                win.Show();
                win.Activate();
            }
            else
            {
                item.Hidden = false;
                _onChanged();
                var newWin = new CountdownWindow(item, _cfg, _onChanged);
                newWin.Show();
            }
            RefreshItems();
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not string id)
                return;
            var item = _cfg.Items.FirstOrDefault(i => i.Id == id);
            if (item == null)
                return;

            var ok = Dialogs.Confirm(this, "删除倒计时",
                $"确定要删除倒计时「{item.Name}」吗？此操作不可恢复。",
                "删除", "取消");
            if (!ok)
                return;

            _cfg.Items.RemoveAll(i => i.Id == id);
            CountdownWindow.All.FirstOrDefault(w => w.Item.Id == id)?.Close();
            _onChanged();
            RefreshItems();
        }

        private void AutoStartToggle_Checked(object sender, RoutedEventArgs e)
        {
            AutoStartHelper.Set(true);
            _cfg.AutoStart = true;
            _onChanged();
        }

        private void AutoStartToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            AutoStartHelper.Set(false);
            _cfg.AutoStart = false;
            _onChanged();
        }

        private void Done_Click(object sender, RoutedEventArgs e) => Close();

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
