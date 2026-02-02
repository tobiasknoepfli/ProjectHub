using System.Windows;

namespace Sleipnir.App.Views
{
    public partial class LogoutConfirmationWindow : Window
    {
        public LogoutConfirmationWindow()
        {
            InitializeComponent();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
