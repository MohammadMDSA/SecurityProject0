using System;

using SecurityProject0_shared.Models;
using SecurityProject0_client.Core.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.ComponentModel;
using SecurityProject0_client.Services;
using SecurityProject0_client.Core.Helpers;
using SecurityProject0_client.Models;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Data;

namespace SecurityProject0_client.Views
{
    public sealed partial class ChatsDetailControl : UserControl
    {
        private UserDataService UserDataService = Singleton<UserDataService>.Instance;
        private ObservableCollection<Message> Messages = new ObservableCollection<Message>();

        public Contact MasterMenuItem
        {
            get { return GetValue(MasterMenuItemProperty) as Contact ?? new Contact("", -1); }
            set { SetValue(MasterMenuItemProperty, value); }
        }

        public static readonly DependencyProperty MasterMenuItemProperty = DependencyProperty.Register("MasterMenuItem", typeof(Contact), typeof(ChatsDetailControl), new PropertyMetadata(null, OnMasterMenuItemPropertyChanged));

        public ChatsDetailControl()
        {
            InitializeComponent();
            MessageParser.OnMessage += MessageParser_OnMessage;
            Binding b = new Binding();
            b.Source = Messages;
            b.Mode = BindingMode.OneWay;
            ChatList.SetBinding(ListView.ItemsSourceProperty, b);
            Loaded += ChatsDetailControl_Loaded;
        }

        private void ChatsDetailControl_Loaded(object sender, RoutedEventArgs e)
        {
            Messages.Clear();
            foreach (var item in MasterMenuItem.Messages)
            {
                Messages.Add(item);
            }
        }

        private async void MessageParser_OnMessage(Message msg, int sender, int receiver)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (sender != MasterMenuItem.Id && receiver != MasterMenuItem.Id)
                    return;
                Messages.Add(msg);
            });
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
