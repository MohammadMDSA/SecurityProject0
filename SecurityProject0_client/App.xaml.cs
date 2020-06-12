using System;

using SecurityProject0_client.Core.Helpers;
using SecurityProject0_client.Core.Services;
using SecurityProject0_client.Services;

using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;

namespace SecurityProject0_client
{
    public sealed partial class App : Application
    {
        private IdentityService IdentityService => Singleton<IdentityService>.Instance;

        private Lazy<ActivationService> _activationService;

        private ActivationService ActivationService
        {
            get { return _activationService.Value; }
        }

        public App()
        {
            InitializeComponent();

            // Deferred execution until used. Check https://msdn.microsoft.com/library/dd642331(v=vs.110).aspx for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);
            IdentityService.LoggedOut += OnLoggedOut;
            Application.Current.Suspending += Current_Suspending;
            MessageSender.OnIncommeingMessage += SecurityProject0_client.Core.Services.MessageParser.Parse;

        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var def = e.SuspendingOperation.GetDeferral();
            //await System.Threading.Tasks.Task.Run(() => { MessageSender.Instance.SendMessage("disconnect"); });
            MessageSender.Instance.Dispose();
            def.Complete();

        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!args.PrelaunchActivated)
            {
                await ActivationService.ActivateAsync(args);
            }
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await ActivationService.ActivateAsync(args);
        }

        private ActivationService CreateActivationService()
        {
            return new ActivationService(this, typeof(Views.ChatsPage), new Lazy<UIElement>(CreateShell));
        }

        private UIElement CreateShell()
        {
            return new Views.ShellPage();
        }

        private async void OnLoggedOut(object sender, EventArgs e)
        {
            ActivationService.SetShell(new Lazy<UIElement>(CreateShell));
            await ActivationService.RedirectLoginPageAsync();
        }

    }
}
