using System;
using System.Windows;
using System.Windows.Controls;
using Sleipnir.App.ViewModels;

namespace Sleipnir.App.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.OnLoginSuccess += (s, e) => 
            {
                this.DialogResult = true;
                this.Close();
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        private void LoginPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }

        private void RegPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.RegPassword = ((PasswordBox)sender).Password;
            }
        }
    }
}
