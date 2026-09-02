using System;
using System.Threading;
using System.Windows;

namespace DayCountdown;

public partial class App : Application
{
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "DayCountdown_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var cfg = ConfigStore.Load();

        // 首次启动：创建一个默认倒计时
        if (cfg.Items.Count == 0)
        {
            cfg.Items.Add(new CountdownItem
            {
                Name = "倒计时",
                TargetDate = DateTime.Today.AddDays(7)
            });
            ConfigStore.Save(cfg);
        }
        else if (cfg.Items.All(i => i.Hidden))
        {
            // 所有记录都被隐藏：自动恢复第一条，避免启动后无任何窗口可显示
            cfg.Items[0].Hidden = false;
            ConfigStore.Save(cfg);
        }

        // 恢复所有可见的倒计时窗口（已关闭/隐藏的记录保留，可在设置中重新打开）
        foreach (var item in cfg.Items)
        {
            if (item.Hidden)
                continue;
            var win = new CountdownWindow(item, cfg, () => ConfigStore.Save(cfg));
            win.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
