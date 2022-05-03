using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Interaction logic for Light.xaml
    /// </summary>
    public partial class Item : UserControl
    {
        public event EventHandler<int> Delete;

        public event EventHandler<int> Enter;
        public int Index { get; set; }
        private ItemType type { get; set; }
        public ItemType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
                switch (Type)
                {
                    case ItemType.Light:
                        ImgOFF = @"/img/Bulb_off.png";
                        ImgON = @"/img/Bulb_b.png";
                        break;
                    case ItemType.Socket:
                        ImgOFF = @"/img/Socket_off.png";
                        ImgON = @"/img/Socket_b.png";
                        break;
                    case ItemType.Temp:
                        ImgOFF = @"/img/Temp_off.png";
                        ImgON = @"/img/Temp.png";
                        break;
                    case ItemType.Humi:
                        ImgOFF = @"/img/Humi_off.png";
                        ImgON = @"/img/Humi.png";
                        break;
                    case ItemType.CO2:
                        ImgOFF = @"/img/CO2_off.png";
                        ImgON = @"/img/CO2.png";
                        break;
                    case ItemType.Unknow:
                        ImgOFF = @"/img/unknow_off.png";
                        ImgON = @"/img/unknow.png";
                        break;
                    default:
                        ImgOFF = @"/img/unknow_off.png";
                        ImgON = @"/img/unknow.png";
                        break;
                }
            }
        }
        private double val { get; set; }
        public double Val
        {
            get
            {
                return val;
            }
            set
            {
                val = value;

                this.Dispatcher.Invoke((new Action(() =>
                {
                    if (Val > 0)
                        Img.Source = new BitmapImage(new Uri(ImgON, UriKind.Relative));
                    else
                        Img.Source = new BitmapImage(new Uri(ImgOFF, UriKind.Relative));

                    switch (Type)
                    {
                        case ItemType.Light:
                            ValString = (Val > 0) ? "ON" : "OFF";
                            break;
                        case ItemType.Socket:
                            ValString = Val.ToString() + " W";

                            break;
                        case ItemType.Temp:
                            ValString = Val.ToString() + " °C";
                            break;
                        case ItemType.Humi:
                            ValString = Val.ToString() + " %";
                            break;
                        case ItemType.CO2:
                            ValString = Val.ToString() + " %";
                            break;
                        case ItemType.Unknow:
                            ValString = Val.ToString() + " [-]";
                            break;
                        default:
                            break;
                    }
                })));





            }
        }
        private string valString { get; set; }
        public string ValString
        {
            get
            {
                return valString;
            }
            set
            {
                valString = value;
                ValLabel.Text = valString;
            }
        }
        private string name { get; set; }
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                NameLabel.Text = name;
            }
        }
        public string ImgON { get; set; }
        public string ImgOFF { get; set; }
        public string MqttTopic { get; set; }
        public string CommandON { get; set; }
        public string CommandOFF { get; set; }
        public string CommandRead { get; set; }
        public string Command
        {
            get
            {
                if (Val > 0) 
                    return CommandOFF;
                else 
                    return CommandON;
            }
        }

        public Item(int index, ItemType typ)
        {
            InitializeComponent();

            Name = "LIGHT #" + index.ToString();

            Index = index;
            Type = typ;
            Val = 0;
        }

        public Item(int index, ExportClasses.JsonItem copy)
        {
            InitializeComponent();

            Name = copy.nam;
            Index = index;
            Type = copy.typ;
            Val = copy.val;
            MqttTopic = copy.top;
            CommandON = copy.on;
            CommandOFF = copy.off;
            CommandRead = copy.red;
            DisableDelete();
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DeleteBorder.Visibility == Visibility.Hidden)
                SwitchValue();
            ItemClicked();
        }
        public void SwitchValue()
        {
            //if (Val == 0)
            //    Val = 1;
            //else
            //    Val = 0;
        }
        public void InsertValue(int newVal)
        {
            Val = newVal;
        }
        protected virtual void DeleteClicked() //protected virtual method
        {
            //if ProcessCompleted is not null then call delegate
            Delete?.Invoke(this, this.Index);
        }
        protected virtual void ItemClicked() //protected virtual method
        {
            Enter?.Invoke(this, this.Index);
        }
        private void DeleteBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DeleteClicked();
        }
        public void EnableDelete()
        {
            DeleteBorder.Visibility = Visibility.Visible;
        }
        public void DisableDelete()
        {
            DeleteBorder.Visibility = Visibility.Hidden;
        }
    }
}
