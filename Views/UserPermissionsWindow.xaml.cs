using System.Windows;
using Sleipnir.App.Models;

namespace Sleipnir.App.Views
{
    public partial class UserPermissionsWindow : Window
    {
        private readonly ViewModels.MainViewModel _viewModel;

        public ViewModels.MainViewModel ViewModel { get; }

        public UserPermissionsWindow(AppUser user, ViewModels.MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = user;
            ViewModel = viewModel;
            _viewModel = viewModel;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AppUser user)
            {
                await _viewModel.SaveUserCommand.ExecuteAsync(user);
            }
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
