using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using SecurityProject0_client.Core.Helpers;
using SecurityProject0_client.Core.Services;
using SecurityProject0_client.Helpers;
using SecurityProject0_client.Models;
using SecurityProject0_client.Services;

using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SecurityProject0_client.Views
{
    // TODO WTS: Add other settings as necessary. For help see https://github.com/Microsoft/WindowsTemplateStudio/blob/master/docs/pages/settings-codebehind.md
    // TODO WTS: Change the URL for your privacy policy in the Resource File, currently set to https://YourPrivacyUrlGoesHere
    public sealed partial class SettingsPage : Page, INotifyPropertyChanged
    {
        private UserDataService UserDataService => Singleton<UserDataService>.Instance;

        private IdentityService IdentityService => Singleton<IdentityService>.Instance;

        private ElementTheme _elementTheme = ThemeSelectorService.Theme;

        public ElementTheme ElementTheme
        {
            get { return _elementTheme; }

            set { Set(ref _elementTheme, value); }
        }

        private string _versionDescription;

        public string VersionDescription
        {
            get { return _versionDescription; }

            set { Set(ref _versionDescription, value); }
        }

        private UserData _user;

        public UserData User
        {
            get { return _user; }
            set { Set(ref _user, value); }
        }

        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            VersionDescription = GetVersionDescription();
            User = await UserDataService.GetUserAsync();
        }

        private string GetVersionDescription()
        {
            var appName = "AppDisplayName".GetLocalized();
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"{appName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private async void ThemeChanged_CheckedAsync(object sender, RoutedEventArgs e)
        {
            var param = (sender as RadioButton)?.CommandParameter;

            if (param != null)
            {
                await ThemeSelectorService.SetThemeAsync((ElementTheme)param);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            UnregisterEvents();
        }

        public void UnregisterEvents()
        {
            IdentityService.LoggedOut -= OnLoggedOut;
            UserDataService.UserDataUpdated -= OnUserDataUpdated;
        }

        private void OnUserDataUpdated(object sender, UserData userData)
        {
            User = userData;
        }

        private async void OnLogout(object sender, RoutedEventArgs e)
        {
            await IdentityService.LogoutAsync();
        }

        private void OnLoggedOut(object sender, EventArgs e)
        {
            UnregisterEvents();
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
