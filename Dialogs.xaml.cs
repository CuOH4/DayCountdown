using System;
using System.Windows;
using System.Windows.Input;

namespace DayCountdown
{
    /// <summary>现代风格对话框快捷入口</summary>
    public static class Dialogs
    {
        /// <summary>文本输入对话框，取消返回 null</summary>
        public static string? Prompt(Window owner, string title, string initial)
        {
            var dlg = new DialogWindow(title, initial) { Owner = owner };
            dlg.ShowDialog();
            return dlg.Confirmed ? dlg.ResultText : null;
        }

        /// <summary>日期选择对话框，取消返回 null</summary>
        public static DateTime? PickDate(Window owner, string title, DateTime initial)
        {
            var dlg = new DialogWindow(title, null, initial) { Owner = owner };
            dlg.ShowDialog();
            return dlg.Confirmed ? dlg.ResultDate : null;
        }

        /// <summary>确认对话框，返回用户是否确认</summary>
        public static bool Confirm(Window owner, string title, string message,
                                   string okText = "确定", string cancelText = "取消")
        {
            var dlg = new ConfirmDialog(title, message, okText, cancelText) { Owner = owner };
            dlg.ShowDialog();
            return dlg.Confirmed;
        }
    }

    public partial class DialogWindow : Window
    {
        public bool Confirmed { get; private set; }
        public string ResultText => InputTextBox.Text.Trim();
        public DateTime ResultDate => InputDatePicker.SelectedDate ?? DateTime.Today;

        public DialogWindow(string title, string? initialText, DateTime? initialDate = null)
        {
            InitializeComponent();
            CaptionText.Text = title;

            if (initialText != null)
            {
                InputTextBox.Visibility = Visibility.Visible;
                InputTextBox.Text = initialText;
                InputTextBox.SelectAll();
                InputTextBox.Focus();
            }
            else
            {
                DatePickerHost.Visibility = Visibility.Visible;
                InputDatePicker.Visibility = Visibility.Visible;
                InputDatePicker.SelectedDate = initialDate ?? DateTime.Today;
                Loaded += (_, _) => InputDatePicker.Focus();
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Ok_Click(sender, e);
        }
    }
}
