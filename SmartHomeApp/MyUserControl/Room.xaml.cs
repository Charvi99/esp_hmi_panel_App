using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SmartHomeApp.MyUserControl
{
    public delegate void Notify();  // delegate
    /// <summary>
    /// Interaction logic for Room.xaml
    /// </summary>
    public partial class Room : UserControl, INotifyPropertyChanged
    {
        public event EventHandler<int> Delete;

        public event EventHandler<int> Enter;

        private bool active;
        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                active = value;
                OnPropertyChanged(nameof(Active));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private List<Item> items;
        public List<Item> Items
        {
            get
            {
                return items;
            }
            set
            {
                items = value;
                OnPropertyChanged(nameof(Items));
            }
        }

        //pricitat v exit funkci active light count a nasledne nastavit automaticky zobrazeni stavu a hodnoty :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public Image SidePanelLight_img { get; set; }
        public TextBlock SidePanelLight_info { get; set; }
        public int LightCount { get; set; }
        private int activeLightCount { get; set; }
        public int ActiveLightCount
        {
            get
            {
                return activeLightCount;
            }
            set
            {
                activeLightCount = value;
                this.Dispatcher.Invoke((new Action(() =>
                {

                    if (activeLightCount > 0)
                    {
                        light_img.Source = new BitmapImage(new Uri(@"/img/Bulb_b.png", UriKind.Relative));
                        SidePanelLight_img.Source = new BitmapImage(new Uri(@"/img/Bulb_b.png", UriKind.Relative));
                    }
                    else
                    {
                        light_img.Source = new BitmapImage(new Uri(@"/img/Bulb_off.png", UriKind.Relative));
                        SidePanelLight_img.Source = new BitmapImage(new Uri(@"/img/Bulb_off.png", UriKind.Relative));
                    }
                    lightInfo.Text = ActiveLightCount.ToString() + "/" + LightCount;
                    SidePanelLight_info.Text = ActiveLightCount.ToString() + "/" + LightCount;
                })));
            }
        }
        public Image SidePanelSocket_img { get; set; }
        public TextBlock SidePanelSocket_info { get; set; }
        public int SocketCount { get; set; }
        private int activeSocketCount { get; set; }
        public int ActiveSocketCount
        {
            get
            {
                return activeSocketCount;
            }
            set
            {
                activeSocketCount = value;
                this.Dispatcher.Invoke((new Action(() =>
                {

                    if (ActiveSocketCount > 0)
                    {
                        socket_img.Source = new BitmapImage(new Uri(@"/img/Socket_b.png", UriKind.Relative));
                        SidePanelSocket_img.Source = new BitmapImage(new Uri(@"/img/Socket_b.png", UriKind.Relative));
                    }
                    else
                    {
                        socket_img.Source = new BitmapImage(new Uri(@"/img/Socket_off.png", UriKind.Relative));
                        SidePanelSocket_img.Source = new BitmapImage(new Uri(@"/img/Socket_off.png", UriKind.Relative));
                    }

                    socketInfo.Text = ActiveSocketCount.ToString() + "W";
                    SidePanelSocket_info.Text = ActiveSocketCount.ToString() + "W";
                })));
            }
        }
        public int ExtraCount { get; set; }

        public bool deleteFlag { get; set; }
        public int Index { get; set; }
        public string RoomName { get; set; }
        public Room(int index)
        {
            InitializeComponent();
            Items = new List<Item>();
            RoomName = "Room" + index;
            RoomName_label.Text = RoomName;

            lightInfo.Text = "0/0";
            socketInfo.Text = "0W";
            Index = index;
            Active = false;
            DeleteBorder.Visibility = Visibility.Hidden;
            deleteFlag = false;

            light_img.Source = new BitmapImage(new Uri(@"/img/Bulb_off.png", UriKind.Relative));
            socket_img.Source = new BitmapImage(new Uri(@"/img/Socket_off.png", UriKind.Relative));

        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        protected virtual void DeleteClicked() //protected virtual method
        {
            //if ProcessCompleted is not null then call delegate
            Delete?.Invoke(this, this.Index);
        }
        protected virtual void EnterRoom() //protected virtual method
        {
            //if ProcessCompleted is not null then call delegate
            Enter?.Invoke(this, this.Index);
        }


        public ((int, int), (int, int)) DisplaySumaryItemVals(Grid sidePanel)
        {
            this.Dispatcher.Invoke((new Action(() =>
            {
                SidePanelLight_img = (Image)((Grid)(sidePanel.Children[2])).Children[0];
                SidePanelLight_info = (TextBlock)((Grid)(sidePanel.Children[2])).Children[1];

                SidePanelSocket_img = (Image)((Grid)(sidePanel.Children[3])).Children[0];
                SidePanelSocket_info = (TextBlock)((Grid)(sidePanel.Children[3])).Children[1];
            })));

            ActiveLightCount = 0;
            ActiveSocketCount = 0;

            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Type == ItemType.Light)
                {
                    ActiveLightCount += (int)Items[i].Val;
                }
                else if (Items[i].Type == ItemType.Socket)
                {
                    ActiveSocketCount += (int)Items[i].Val;
                }

            }

            return ((ActiveLightCount, LightCount), (ActiveSocketCount, SocketCount));
        }


        public void EnableDelete()
        {
            DeleteBorder.Visibility = Visibility.Visible;
        }
        public void DisableDelete()
        {
            DeleteBorder.Visibility = Visibility.Hidden;
        }

        private void DeleteBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            deleteFlag = true;
            DeleteClicked();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DeleteBorder.Visibility == Visibility.Hidden)
                EnterRoom();
        }
    }
}


