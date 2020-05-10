using System;

using SecurityProject0_shared.Models;
using SecurityProject0_client.Core.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SecurityProject0_client.Views
{
    public sealed partial class ChatsDetailControl : UserControl
    {
        public Message MasterMenuItem
        {
            get { return GetValue(MasterMenuItemProperty) as Message; }
            set { SetValue(MasterMenuItemProperty, value); }
        }

        public static readonly DependencyProperty MasterMenuItemProperty = DependencyProperty.Register("MasterMenuItem", typeof(Message), typeof(ChatsDetailControl), new PropertyMetadata(null, OnMasterMenuItemPropertyChanged));

        public ChatsDetailControl()
        {
            InitializeComponent();
        }

        private static void OnMasterMenuItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ChatsDetailControl;
            //control.ForegroundElement.ChangeView(0, 0, 1);
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
            MessageSender.Connect("127.0.0.1", message);
        }
    }
}
