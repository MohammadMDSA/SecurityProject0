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

namespace SecurityProject0_client.Views
{
    public sealed partial class ChatsPage : Page, INotifyPropertyChanged
    {
        private Contact _selected;

        public Contact Selected
        {
            get { return _selected; }
            set { Set(ref _selected, value); }
        }

        public ObservableCollection<Contact> SampleItems { get; private set; } = new ObservableCollection<Contact>();

        public ChatsPage()
        {
            InitializeComponent();
            Loaded += ChatsPage_Loaded;
        }

        private async void ChatsPage_Loaded(object sender, RoutedEventArgs e)
        {
            SampleItems.Clear();

            var chats = new List<Message> {
                new Message { DeliveryTime = DateTime.Now, FromMe = true, _rawMessage = "سلام"},
                new Message { DeliveryTime = DateTime.Now - TimeSpan.FromHours(1), FromMe = false, _rawMessage = "سلام1"}
            };

            var contacts = new[]
            {
                new Contact{Name="Mohammad", Messages = chats}
            };

            foreach (var item in contacts)
            {
                SampleItems.Add(item);
            }

            if (MasterDetailsViewControl.ViewState == MasterDetailsViewState.Both)
            {
                Selected = SampleItems.FirstOrDefault();
            }
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
