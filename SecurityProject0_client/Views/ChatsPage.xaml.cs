using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.Toolkit.Uwp.UI.Controls;

using SecurityProject0_shared.Models;
using SecurityProject0_client.Core.Services;
using SecurityProject0_client.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using System.Collections.Concurrent;

namespace SecurityProject0_client.Views
{
    public sealed partial class ChatsPage : Page, INotifyPropertyChanged
    {

        public static ChatsPage Instance;

        private Contact _selected;

        public Contact Selected
        {
            get { return _selected; }
            set { Set(ref _selected, value); }
        }

        public ObservableCollection<Contact> Contacts { get; private set; } = new ObservableCollection<Contact>();

        public ChatsPage()
        {
            Instance = this;
            InitializeComponent();
            Loaded += ChatsPage_Loaded;
        }

        //private void GlobalContacts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        //{
        //    //if (GlobalContacts.Count == 0)
        //    //    return;
        //    Contacts.Clear();
        //    foreach (var item in GlobalContacts)
        //    {
        //        Contacts.Add(item);
        //    }
        //    //MasterDetailsViewControl.ItemsSource = GlobalContacts;
        //    //MasterDetailsViewControl.ItemsSource = new List<Contact> { new Contact("ali", 1) };
        //}

        private async void ChatsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.CompletedTask;
            //Contacts.Add(new Contact("ali", 1));
            //var chats = new List<Message> {
            //    new Message { DeliveryTime = DateTime.Now, FromMe = true, _rawMessage = "سلام"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"},
            //    new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"}
            //};

            //var contacts = new[]
            //{
            //    new Contact("Mohammad", 1) { Messages = chats}
            //};

            //foreach (var item in contacts)
            //{
            //    Contacts.Add(item);
            //}

            var contacts = from item in MessageParser.Contacts
                           select item.Value;
            foreach (var item in contacts)
            {
                Contacts.Add(item);
            }

            if (MasterDetailsViewControl.ViewState == MasterDetailsViewState.Both)
            {
                Selected = Contacts.FirstOrDefault();
            }

        }

        public void Add(Contact con)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Contacts.Add(con);

            });
        }

        public void Remove(int conId)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Contacts.Remove(new Contact("", conId));

            });
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
