using System;
using System.Windows;
using System.Windows.Input;

namespace DayCountdown
{
    public partial class NewCountdownDialog : Window
    {
        public string ResultName => NameTextBox.Text.Trim();
        public DateTime ResultDate => DatePickerControl.SelectedDate ?? DateTime.Today.AddDays(7);

        public NewCountdownDialog()
        {
            InitializeComponent();
            DatePickerControl.SelectedDate = DateTime.Today.AddDays(7);
            Loaded += (_, _) => NameTextBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Ok_Click(sender, e);
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
