using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sleipnir.App.Models;
using Sleipnir.App.Services;
using Sleipnir.App.Utils;

namespace Sleipnir.App.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isRegistrationVisible;

        [ObservableProperty]
        private string _regFirstName = string.Empty;

        [ObservableProperty]
        private string _regLastName = string.Empty;

        [ObservableProperty]
        private string _regEmail = string.Empty;

        [ObservableProperty]
        private string _regUsername = string.Empty;

        [ObservableProperty]
        private string _regPassword = string.Empty;

        [ObservableProperty]
        private string _regSelectedEmoji = "Account";

        [ObservableProperty]
        private bool _isPasswordResetVisible;

        [ObservableProperty]
        private string _resetUsername = string.Empty;

        [ObservableProperty]
        private string _resetEmail = string.Empty;

        [ObservableProperty]
        private string _emojiSearchQuery = string.Empty;

        [ObservableProperty]
        private bool _rememberMe;

        public ObservableCollection<string> AvailableEmojis { get; } = new();
        private List<IconItem> _allEmojis = new();

        public AppUser? AuthenticatedUser { get; private set; }

        public LoginViewModel(IDataService dataService)
        {
            _dataService = dataService;
            LoadEmojis();
        }

        private async void LoadEmojis()
        {
            _allEmojis = EmojiHelper.GetAllEmojis();
            
            // Filter out taken emojis
            try 
            {
                var users = await _dataService.GetUsersAsync();
                var takenEmojis = users.Select(u => u.Emoji).ToHashSet();
                _allEmojis = _allEmojis.Where(e => !takenEmojis.Contains(e.Id)).ToList();
            }
            catch { /* Fallback if service not ready */ }

            RefreshEmojiList();
        }

        [RelayCommand]
        private void RefreshEmojiList()
        {
            AvailableEmojis.Clear();
            var filtered = string.IsNullOrWhiteSpace(EmojiSearchQuery) 
                ? _allEmojis 
                : _allEmojis.Where(e => e.Id.Contains(EmojiSearchQuery, StringComparison.OrdinalIgnoreCase) || e.Name.Contains(EmojiSearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var e in filtered.Take(5000)) AvailableEmojis.Add(e.Id);
        }

        partial void OnEmojiSearchQueryChanged(string value) => RefreshEmojiList();

        [RelayCommand]
        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;
            var user = await _dataService.GetUserByUsernameAsync(Username);

            if (user != null && user.Password == Password)
            {
                AuthenticatedUser = user;
                // Handle Auto-login preference
                if (RememberMe)
                {
                    user.CanAutoLogin = true;
                    await _dataService.UpdateUserAsync(user);
                }
                
                OnLoginSuccess?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = "Invalid username or password.";
            }
        }

        [RelayCommand]
        private void ShowRegistration() => IsRegistrationVisible = true;

        [RelayCommand]
        private void ShowLogin() => IsRegistrationVisible = false;

        [RelayCommand]
        private async Task RegisterAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(RegUsername) || string.IsNullOrWhiteSpace(RegPassword))
            {
                ErrorMessage = "Username and Password are required.";
                return;
            }

            var existing = await _dataService.GetUserByUsernameAsync(RegUsername);
            if (existing != null)
            {
                ErrorMessage = "Username already exists.";
                return;
            }

            var newUser = new AppUser
            {
                FirstName = RegFirstName,
                LastName = RegLastName,
                Email = RegEmail,
                Username = RegUsername,
                Password = RegPassword,
                Emoji = RegSelectedEmoji,
                IsSuperuser = false
            };

            await _dataService.CreateUserAsync(newUser);
            IsRegistrationVisible = false;
            Username = RegUsername;
            Password = RegPassword;
        }

        [RelayCommand]
        private void ShowPasswordReset()
        {
            IsPasswordResetVisible = true;
            IsRegistrationVisible = false;
        }

        [RelayCommand]
        private async Task ResetPasswordAsync()
        {
            ErrorMessage = string.Empty;
            if (string.IsNullOrEmpty(ResetUsername) || string.IsNullOrEmpty(ResetEmail))
            {
                ErrorMessage = "Please provide both username and email.";
                return;
            }

            var user = await _dataService.GetUserByUsernameAsync(ResetUsername);
            if (user != null && !string.IsNullOrEmpty(user.Email) && 
                user.Email.Equals(ResetEmail, StringComparison.OrdinalIgnoreCase))
            {
                // Safe way for superuser reset
                user.Password = "admin"; // Reset to default
                await _dataService.UpdateUserAsync(user);
                ErrorMessage = "Password has been reset to 'admin'. Please login and change it immediately.";
                IsPasswordResetVisible = false;
            }
            else
            {
                ErrorMessage = "Verification failed. Username or email did not match.";
            }
        }

        public event EventHandler? OnLoginSuccess;
    }
}
