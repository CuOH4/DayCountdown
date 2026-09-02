using System.Windows;
using System.Windows.Input;

namespace DayCountdown
{
    /// <summary>与界面风格一致的确认对话框</summary>
    public partial class ConfirmDialog : Window
    {
        public bool Confirmed { get; private set; }

        public ConfirmDialog(string title, string message, string okText = "确定", string cancelText = "取消")
        {
            InitializeComponent();
            CaptionText.Text = title;
            MessageText.Text = message;
            OkButton.Content = okText;
            CancelButton.Content = cancelText;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1) return;
            DragMove();
        }
    }
}
