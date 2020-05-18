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
            Binding b = new Binding();
            b.Source = Messages;
            b.Mode = BindingMode.OneWay;
            ChatList.SetBinding(ListView.ItemsSourceProperty, b);
            Loaded += ChatsDetailControl_Loaded;
            OnMasterChange += ChatsDetailControl_OnMasterChange;
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
            MessageSender.Instance.SendMessage($"message{Helper.SocketMessageSeperator}{MasterMenuItem.Id}{Helper.SocketMessageSeperator}{message}{Helper.SocketMessageSeperator}{DateTime.Now.Ticks}");
        }

        private void SubmitKey_Click(object sender, RoutedEventArgs e)
        {
            MasterMenuItem.Secret = ContactPhisical.Text;
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

                    var file = await FileIO.ReadTextAsync(item as IStorageFile, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);
                    MessageSender.Instance.SendMessage($"file{Helper.SocketMessageSeperator}{MasterMenuItem.Id}{Helper.SocketMessageSeperator}{item.Name};{file}{Helper.SocketMessageSeperator}{DateTime.Now.Ticks}");
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
    }
}
