using System;

using SecurityProject0_shared.Models;
using SecurityProject0_client.Core.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.ComponentModel;
using SecurityProject0_client.Services;
using SecurityProject0_client.Core.Helpers;
using SecurityProject0_client.Models;

namespace SecurityProject0_client.Views
{
    public sealed partial class ChatsDetailControl : UserControl
    {
        private UserDataService UserDataService = Singleton<UserDataService>.Instance;

        public Contact MasterMenuItem
        {
            get { return GetValue(MasterMenuItemProperty) as Contact ?? new Contact("", -1); }
            set { SetValue(MasterMenuItemProperty, value); }
        }

        public static readonly DependencyProperty MasterMenuItemProperty = DependencyProperty.Register("MasterMenuItem", typeof(Contact), typeof(ChatsDetailControl), new PropertyMetadata(null, OnMasterMenuItemPropertyChanged));

        public ChatsDetailControl()
        {
            InitializeComponent();
        }


        private static void OnMasterMenuItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ChatsDetailControl;
            control.ChatViewScroller.ChangeView(0, 0, 1);
        }

        private void TextBox_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                SendText();
            e.Handled = true;
        }

        public void SendText()
        {
            var message = MessageInput.Text;
            MessageInput.Text = string.Empty;
            var user = UserDataService.GetUserData();
            MessageSender.Instance.SendMessage($"message@{MasterMenuItem.Id}@{message}@{DateTime.Now.Ticks}");
        }

        private void SubmitKey_Click(object sender, RoutedEventArgs e)
        {
            MasterMenuItem.Secret = ContactPhisical.Text;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendText();
        }
    }
}
