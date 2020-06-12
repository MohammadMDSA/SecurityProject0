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
using System.Collections.Generic;
using Windows.Storage;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Windows.Storage.Pickers;
using SecurityProject0_client.Helpers;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.Foundation;

namespace SecurityProject0_client.Views
{
    public sealed partial class ChatsDetailControl : UserControl
    {

        public static event EventHandler OnMasterChange;

        private UserDataService UserDataService = Singleton<UserDataService>.Instance;
        private ObservableCollection<Message> Messages = new ObservableCollection<Message>();

        public Contact MasterMenuItem
        {
            get { return GetValue(MasterMenuItemProperty) as Contact ?? new Contact("", -1); }
            set { SetValue(MasterMenuItemProperty, value); }
        }

        public readonly DependencyProperty MasterMenuItemProperty = DependencyProperty.Register("MasterMenuItem", typeof(Contact), typeof(ChatsDetailControl), new PropertyMetadata(null, OnMasterMenuItemPropertyChanged));
        public Action<IReadOnlyList<IStorageItem>> GetStorageItem => ((items) => OnGetStorageItem(items));

        public ChatsDetailControl()
        {
            InitializeComponent();
            MessageParser.OnMessage += MessageParser_OnMessage;
            MessageParser.OnNewFile += FileSaver.SaveFile;
            MessageParser.OnPhysicalKeyChanged += MessageParser_OnPhysicalKeyChanged;
            Binding b = new Binding();
            b.Source = Messages;
            b.Mode = BindingMode.OneWay;
            ChatList.SetBinding(ListView.ItemsSourceProperty, b);
            Loaded += ChatsDetailControl_Loaded;
            OnMasterChange += ChatsDetailControl_OnMasterChange;
            ChatList.ContainerContentChanging += ChatList_ContainerContentChanging;
        }

        private void ChatList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            args.RegisterUpdateCallback((senderi, argsi) =>
            {
                ScrollToBottom();

            });
        }

        private async void MessageParser_OnPhysicalKeyChanged(string key)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //ContactPhisical.Text = key;
            });
        }

        public async void OnGetStorageItem(IReadOnlyList<IStorageItem> items)
        {
            await SendFiles(items);
        }

        private void ChatsDetailControl_OnMasterChange(object sender, EventArgs e)
        {
            ContactName.Text = MasterMenuItem.Name;
            Messages.Clear();
            foreach (var item in MasterMenuItem.Messages)
            {
                Messages.Add(item);
            }
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
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                if (sender != MasterMenuItem.Id && receiver != MasterMenuItem.Id)
                    return;
                Messages.Add(msg);
                Task.Delay(200).Wait();

            });
        }

        private static void OnMasterMenuItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ChatsDetailControl;
            control.ChatViewScroller.ChangeView(0, 0, 1);
            OnMasterChange?.Invoke(null, EventArgs.Empty);
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
            MessageSender.Instance.SendMessage($"message{Helper.SocketMessageAttributeSeperator}{MasterMenuItem.Id}{Helper.SocketMessageAttributeSeperator}{message}{Helper.SocketMessageAttributeSeperator}{DateTime.Now.Ticks}");
        }

        private void SubmitKey_Click(object sender, RoutedEventArgs e)
        {
            //MasterMenuItem.Secret = ContactPhisical.Text;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendText();

        }

        private async Task SendFiles(IReadOnlyList<IStorageItem> items)
        {
            foreach (var item in items)
            {
                try
                {
                    var bytes = await (item as StorageFile).ReadBytesAsync();
                    var file = Convert.ToBase64String(bytes);
                    //var file = await FileIO.ReadTextAsync(item as IStorageFile, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                    MessageSender.Instance.SendMessage($"file{Helper.SocketMessageAttributeSeperator}{MasterMenuItem.Id}{Helper.SocketMessageAttributeSeperator}{item.Name};{file}{Helper.SocketMessageAttributeSeperator}{DateTime.Now.Ticks}");
                }
                catch (Exception) { }

            }
        }

        private async void AttachButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.CommitButtonText = "Send";
            picker.FileTypeFilter.Add("*");
            var files = await picker.PickMultipleFilesAsync();
            if (files == null || files.Count == 0)
                return;
            else
            {
                await SendFiles(files);
            }
        }

        private void EncType_Toggled(object sender, RoutedEventArgs e)
        {
            //string type;
            //if (EncType.IsOn)
            //{
            //    MessageSender.Instance.EncryptionMode = EncryptionMode.AES;
            //    type = "sym";
            //}
            //else
            //{
            //    type = "asy";
            //    MessageSender.Instance.EncryptionMode = EncryptionMode.RSA;
            //}
            //MessageSender.Instance.SendMessage($"encryption{Helper.SocketMessageAttributeSeperator}{type}", EncryptionMode.RSA);

        }

        public void ScrollToBottom()
        {
            var last = ChatList.ItemsPanelRoot.Children.LastOrDefault();
            if (last is null || ((dynamic)last).Content is null)
                return;
            var transform = last.TransformToVisual((UIElement)ChatViewScroller.Content);
            var position = transform.TransformPoint(new Point(0, 0));
            ChatViewScroller.ChangeView(null, position.Y, null, false);
        }

    }
}
