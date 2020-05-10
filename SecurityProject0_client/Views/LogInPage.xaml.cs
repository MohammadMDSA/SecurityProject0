using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SecurityProject0_client.Core.Helpers;
using SecurityProject0_client.Core.Services;
using SecurityProject0_client.Helpers;
using SecurityProject0_client.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SecurityProject0_client.Views
{
    public sealed partial class LogInPage : Page, INotifyPropertyChanged
    {
        private string _statusMessage;
        private bool _isBusy;

        private IdentityService IdentityService => Singleton<IdentityService>.Instance;

        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => Set(ref _isBusy, value);
        }

        public LogInPage()
        {
            InitializeComponent();
        }

        private async void OnLogIn(object sender, RoutedEventArgs e)
        {
            IsBusy = true;
            StatusMessage = string.Empty;
            var loginResult = await IdentityService.LoginAsync(NameInput.Text);
            StatusMessage = GetStatusMessage(loginResult);
            IsBusy = false;
        }

        private string GetStatusMessage(LoginResultType loginResult)
        {
            switch (loginResult)
            {
                case LoginResultType.Unauthorized:
                    return "StatusUnauthorized".GetLocalized();
                case LoginResultType.NoNetworkAvailable:
                    return "StatusNoNetworkAvailable".GetLocalized();
                case LoginResultType.UnknownError:
                    return "StatusLoginFails".GetLocalized();
                default:
                    return string.Empty;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
