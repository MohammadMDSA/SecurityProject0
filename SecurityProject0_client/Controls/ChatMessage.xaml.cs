using Microsoft.Identity.Client;
using SecurityProject0_shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SecurityProject0_client.Controls
{
    public sealed partial class ChatMessage : UserControl
    {
        public event TypedEventHandler<ChatMessage, Message> OnEdit;
        public event TypedEventHandler<ChatMessage, Message> OnReply;
        public event TypedEventHandler<ChatMessage, Message> OnDelete;

        public Message Message
        {
            get
            {
                var res = GetValue(MessageProperty) as Message ?? new Message(false, -1);
                UpdateMessageType(res);
                return res;
            }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(Message), typeof(ChatMessage), new PropertyMetadata(new PropertyMetadata(null)));



        public ChatMessage()
        {
            this.InitializeComponent();
            UpdateMessageType(Message);
        }

        private void UpdateMessageType(Message mess)
        {
            var isFile = mess.IsFile;
            if(isFile)
            {
                EditButton.Visibility = Visibility.Collapsed;
                var file = mess as SecurityProject0_shared.Models.File;
                FileShower.Visibility = Visibility.Visible;
                MessageShower.Visibility = Visibility.Collapsed;
                FileNameShower.Text = file.Name;
            }
            else
            {
                EditButton.Visibility = Visibility.Visible;
                FileShower.Visibility = Visibility.Collapsed;
                MessageShower.Visibility = Visibility.Visible;
            }
        }

        private async void FontIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var file = Message as SecurityProject0_shared.Models.File;
            var storageFile = await StorageFile.GetFileFromPathAsync(file.Path);
            var res = await Launcher.LaunchFileAsync(storageFile, new LauncherOptions { });
        }

        private void MessageReply(object sender, RoutedEventArgs e)
        {
            this.OnReply?.Invoke(this, this.Message);
        }

        private void MessageEdit(object sender, RoutedEventArgs e)
        {
            this.OnEdit?.Invoke(this, this.Message);
        }

        private void MessageDelete(object sender, RoutedEventArgs e)
        {
            this.OnDelete?.Invoke(this, this.Message);
        }
    }
}
