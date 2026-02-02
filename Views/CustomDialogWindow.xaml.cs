using System.Windows;

namespace Sleipnir.App.Views
{
    public partial class CustomDialogWindow : Window
    {
        public enum DialogType { Success, Error, Info, Warning }

        public enum CustomDialogResult { Ok, No, Cancel }
        private CustomDialogResult _result = CustomDialogResult.Cancel;

        public CustomDialogWindow(string title, string message, DialogType type = DialogType.Info, string okText = "OK", string? noText = null, string? cancelText = null)
        {
            InitializeComponent();
            TitleText.Text = title.ToUpper();
            MessageText.Text = message;
            OkButton.Content = okText;
            
            if (noText != null)
            {
                NoButton.Content = noText;
                NoButton.Visibility = Visibility.Visible;
            }

            if (cancelText != null)
            {
                CancelButton.Content = cancelText;
                CancelButton.Visibility = Visibility.Visible;
            }

            switch (type)
            {
                case DialogType.Success:
                    IconText.Text = "✅";
                    break;
                case DialogType.Error:
                    IconText.Text = "❌";
                    break;
                case DialogType.Warning:
                    IconText.Text = "⚠️";
                    break;
                default:
                    IconText.Text = "ℹ️";
                    break;
            }
        }

        public static CustomDialogResult Show(string title, string message, DialogType type = DialogType.Info, string okText = "OK", string? noText = null, string? cancelText = null)
        {
            var win = new CustomDialogWindow(title, message, type, okText, noText, cancelText);
            foreach (Window window in Application.Current.Windows)
            {
                if (window.IsVisible && window is MainWindow)
                {
                    win.Owner = window;
                    break;
                }
            }
            win.ShowDialog();
            return win._result;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            _result = CustomDialogResult.Ok;
            this.DialogResult = true;
            this.Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            _result = CustomDialogResult.No;
            this.DialogResult = false;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _result = CustomDialogResult.Cancel;
            this.DialogResult = false;
            this.Close();
        }
    }
}
