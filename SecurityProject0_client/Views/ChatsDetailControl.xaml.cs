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
using System.Reflection;
using Windows.Storage.Search;

namespace SecurityProject0_client.Views
{
    public sealed partial class ChatsDetailControl : UserControl
    {

        public static event EventHandler OnMasterChange;

        private UserDataService UserDataService = Singleton<UserDataService>.Instance;
        private ObservableCollection<Message> Messages = new ObservableCollection<Message>();
        private ContextualMessageOperation CurrentState;
        private Message ContextMessageData;

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
            MessageParser.OnEdit += MessageParser_OnEdit;
            MessageParser.OnNewFile += FileSaver.SaveFile;
            MessageParser.OnDelete += MessageParser_OnDelete;
            MessageParser.OnPhysicalKeyChanged += MessageParser_OnPhysicalKeyChanged;
            Binding b = new Binding();
            b.Source = Messages;
            b.Mode = BindingMode.OneWay;
            ChatList.SetBinding(ListView.ItemsSourceProperty, b);
            Loaded += ChatsDetailControl_Loaded;
            OnMasterChange += ChatsDetailControl_OnMasterChange;
            ChatList.ContainerContentChanging += ChatList_ContainerContentChanging;
            CurrentState = ContextualMessageOperation.None;
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
            SetFocusToInput();

        }

        private void ChatsDetailControl_Loaded(object sender, RoutedEventArgs e)
        {
            Messages.Clear();
            foreach (var item in MasterMenuItem.Messages)
            {
                Messages.Add(item);
            }
            SetFocusToInput();

        }

        private async void MessageParser_OnDelete(int msgId, int sender, int receiver)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (sender != MasterMenuItem.Id && receiver != MasterMenuItem.Id)
                    return;
                var idx = -1;
                for (var i = 0; i < Messages.Count; i++)
                {
                    if (Messages[i].Id == msgId)
                    {
                        idx = i;
                        break;
                    }
                }

                if (idx == -1)
                {
                    return;
                }

                Messages.RemoveAt(idx);

            });
        }

        private async void MessageParser_OnEdit(Message msg, int sender, int receiver)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (sender != MasterMenuItem.Id && receiver != MasterMenuItem.Id)
                    return;
                var idx = Messages.IndexOf(msg);
                Messages[idx] = msg;

            });
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
            {
                SendText();
                SetFocusToInput();
            }
            e.Handled = true;
        }

        public void SendText()
        {
            var message = MessageInput.Text;
            MessageInput.Text = string.Empty;
            this.MessageContextGrid.Visibility = Visibility.Collapsed;
            switch (CurrentState)
            {
                case ContextualMessageOperation.None:
                    MessageSender.Instance.SendMessage($"message{Helper.SocketMessageAttributeSeperator}{MasterMenuItem.Id}{Helper.SocketMessageAttributeSeperator}{message}{Helper.SocketMessageAttributeSeperator}{DateTime.Now.Ticks}");
                    break;
                case ContextualMessageOperation.Edit:
                    MessageSender.Instance.SendMessage($"edit_message{Helper.SocketMessageAttributeSeperator}{MasterMenuItem.Id}{Helper.SocketMessageAttributeSeperator}{message}{Helper.SocketMessageAttributeSeperator}{ContextMessageData.Id}");
                    break;
                case ContextualMessageOperation.Reply:
                    break;
                default:
                    break;
            }
            CurrentState = ContextualMessageOperation.None;
            SetFocusToInput();

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

            this.MessageContextGrid.Visibility = Visibility.Collapsed;
            foreach (var item in items)
            {
                try
                {
                    var bytes = await (item as StorageFile).ReadBytesAsync();
                    var file = Convert.ToBase64String(bytes);
                    MessageSender.Instance.SendMessage($"file{Helper.SocketMessageAttributeSeperator}{MasterMenuItem.Id}{Helper.SocketMessageAttributeSeperator}{item.Name};{file}{Helper.SocketMessageAttributeSeperator}{DateTime.Now.Ticks}");
                }
                catch (Exception) { }

            }
            CurrentState = ContextualMessageOperation.None;
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

        public void ScrollToBottom()
        {
            var last = ChatList.ItemsPanelRoot.Children.LastOrDefault();
            if (last is null || ((dynamic)last).Content is null)
                return;
            var transform = last.TransformToVisual((UIElement)ChatViewScroller.Content);
            var position = transform.TransformPoint(new Point(0, 0));
            ChatViewScroller.ChangeView(null, position.Y, null, false);
        }

        private void ChatMessage_OnEdit(Controls.ChatMessage sender, Message args)
        {
            this.CurrentState = ContextualMessageOperation.Edit;
            ContextType.Text = "Edit:";
            ContextMessage.Text = args.RawMessage;
            MessageContextGrid.Visibility = Visibility.Visible;
            MessageInput.Text = args.RawMessage;
            ContextMessageData = args;
            MessageInput.Focus(FocusState.Programmatic);
            SetFocusToInput();

        }

        private void OnContextCancel(object sender, RoutedEventArgs e)
        {
            MessageContextGrid.Visibility = Visibility.Collapsed;
            this.CurrentState = ContextualMessageOperation.None;
            SetFocusToInput();
        }

        private void SetFocusToInput()
        {

            MessageInput.Focus(FocusState.Programmatic);
            MessageInput.Select(MessageInput.Text.Length, 0);
        }

        private async void ChatMessage_OnDelete(Controls.ChatMessage sender, Message args)
        {
            var approval = new ContentDialog
            {
                Title = "Delete Message",
                Content = "Are you sure to delete the message?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No"
            };

            var result = await approval.ShowAsync();
            if (result == ContentDialogResult.Primary)
                MessageSender.Instance.SendMessage($"delete{Helper.SocketMessageAttributeSeperator}{MasterMenuItem.Id}{Helper.SocketMessageAttributeSeperator}{args.Id}");
        }

        public enum ContextualMessageOperation
        {
            None,
            Edit,
            Reply,
        }
    }
}
