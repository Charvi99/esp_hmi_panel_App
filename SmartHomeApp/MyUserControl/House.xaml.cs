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
using Newtonsoft.Json;
using System.IO;

namespace SmartHomeApp.MyUserControl
{
    /// <summary>
    /// Interaction logic for House.xaml
    /// </summary>
    public partial class House : UserControl, INotifyPropertyChanged
    {
        #region Property
        private List<Room> rooms;
        public List<Room> Rooms
        {
            get
            {
                return rooms;
            }
            set
            {
                rooms = value;
                OnPropertyChanged(nameof(Rooms));
            }
        }
        public int ActiveRoomIndex { get; set; }
        public Room ActiveRoom { get; set; }

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
                    lightInfo.Text = ActiveLightCount.ToString() + "/" + LightCount.ToString();
                    if (activeLightCount > 0)
                        side_panel_bulb_img.Source = new BitmapImage(new Uri(ImgBulbON, UriKind.Relative));
                    else
                        side_panel_bulb_img.Source = new BitmapImage(new Uri(ImgBulbOFF, UriKind.Relative));
                })));
            }
        }
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
                    socketInfo.Text = ActiveSocketCount.ToString() + "/" + SocketCount.ToString();
                    if (activeSocketCount > 0)
                        side_panel_socket_img.Source = new BitmapImage(new Uri(ImgSocketON, UriKind.Relative));
                    else
                        side_panel_socket_img.Source = new BitmapImage(new Uri(ImgSocketOFF, UriKind.Relative));
                })));
            }
        }

        public string ImgBulbON = @"/img/Bulb_b.png";
        public string ImgBulbOFF = @"/img/Bulb_off.png";
        public string ImgSocketON = @"/img/Socket_b.png";
        public string ImgSocketOFF = @"/img/Socket_off.png";

        public string MainTopicThred = "house";
        #endregion

        #region OnPropertyChanged
        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        public event PropertyChangedEventHandler PropertyChanged;


        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion

        public House()
        {
            InitializeComponent();
            Rooms = new List<Room>();

            DisplaySidePanel(sidePanel_overview);
            DisplayMainPanel(mainPanel);


            loadRooms();
        }

        #region Load rooms
        /// <summary>
        /// if file "current_setup.json" exist load setup and display it to grid
        /// </summary>
        private void loadRooms()
        {
            string data_txt = File.ReadAllText(@"current_setup.json");
            ExportClasses.JsonHouse data = JsonConvert.DeserializeObject<ExportClasses.JsonHouse>(data_txt);
            if (data == null)
                return;

            //clear and prepare room list and room grid 
            clearRooms();

            for (int i = 0; i < data.rooms.Count; i++)
            {
                CreateNewRoom_MouseDown(null, null);
                Rooms[i].DisableDelete();

                for (int j = 0; j < data.rooms[i].items.Count; j++)
                {
                    Rooms[i].Items.Add(new Item(j, data.rooms[i].items[j]));
                    Rooms[i].Items[j].Enter += enterItem_ProcessCompleted;
                    Rooms[i].Items[j].Delete += deleteItem_ProcessCompleted;
                    if (MainWindow.mqttClient != null)
                        MainWindow.MQTT_Subscribe(Rooms[i].Items[j].MqttTopic);

                    switch (data.rooms[i].items[j].typ)
                    {
                        case ItemType.Light:
                            Rooms[i].LightCount++;
                            break;
                        case ItemType.Socket:
                            Rooms[i].SocketCount++;
                            break;
                        default:
                            Rooms[i].ExtraCount++;
                            break;
                    }

                }

                Rooms[i].DisplaySumaryItemVals(sidePanel_room);
            }
            DisplaySumaryRoomsVals();
        }
        private void clearRooms()
        {
            roomGrid.Children.Clear();
            roomGrid.RowDefinitions.Clear();
            roomGrid.ColumnDefinitions.Clear();

            Rooms.Clear();
        }
        #endregion

        #region Click event for Room and Item
        /// <summary>
        /// Method invoked from usercontrol Room, by click on "X" on room -&gt; make sure
        /// about delete if room is deleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index"></param>
        public void deleteRoom_ProcessCompleted(object sender, int index)
        {
            if (MessageBoxResult.Yes == MessageBox.Show("You really want to delete room and all items inside?", "Delete room?", MessageBoxButton.YesNo, MessageBoxImage.Question))
            {
                //find rooms to delete
                for (int i = 0; i < Rooms.Count; i++)
                    if (Rooms[i].Index == index)
                        DeleteRoom(i);

                //correct index of rooms
                for (int i = 0; i < Rooms.Count; i++)
                    Rooms[i].Index = i;
                DisplaySumaryRoomsVals();
            }
        }
        /// <summary>
        /// Method invoked from usercontrol Room, by click on border room -> open selected room and show items inside
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index"></param>
        public void enterRoom_ProcessCompleted(object sender, int index)
        {
            //show panels related to room 
            DisplayMainPanel(roomPanel);
            DisplaySidePanel(sidePanel_room);

            RoomName_room.Text = Rooms[index].RoomName;
            displayItemsOfRoom(index);
            ActiveRoomIndex = index;
            ActiveRoom = Rooms[ActiveRoomIndex];
            ActiveRoom.DisplaySumaryItemVals(sidePanel_room);
        }

        /// <summary>
        /// Method invoked from usercontrol Item, by click on "X" on item -> make sure about delete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index"></param>
        public void deleteItem_ProcessCompleted(object sender, int index)
        {
            if (MessageBoxResult.Yes == MessageBox.Show("You really want to delete item?", "Delete item?", MessageBoxButton.YesNo, MessageBoxImage.Question))
            {
                //clear grid of items for new layout
                hideItemsOfRoom(ActiveRoomIndex);

                Item toDelete = (Item)sender;
                toDelete.Enter -= enterItem_ProcessCompleted;
                toDelete.Delete -= deleteItem_ProcessCompleted;
                switch (toDelete.Type)
                {
                    case ItemType.Light:
                        ActiveRoom.LightCount--;
                        break;
                    case ItemType.Socket:
                        ActiveRoom.SocketCount--;
                        break;
                    default:
                        ActiveRoom.ExtraCount--;
                        break;
                }
                MainWindow.MQTT_Unubscribe(toDelete.MqttTopic);
                //item is deleted from room 
                ActiveRoom.Items.Remove(toDelete);
                //indexes of items is corrected
                for (int i = 0; i < ActiveRoom.Items.Count; i++)
                    ActiveRoom.Items[i].Index = i;

                //reload items
                displayItemsOfRoom(ActiveRoomIndex);
                ActiveRoom.DisplaySumaryItemVals(sidePanel_room);
            }
        }
        /// <summary>
        /// Method invoked from usercontrol Item, by click on border item -> send command to topic related to item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index"></param>
        public async void enterItem_ProcessCompleted(object sender, int index)
        {
            switch (sidePanel_room_settings.Visibility)
            {
                //if settings mode is active -> after click fill newitem form with existing data from item
                case Visibility.Visible:
                    BorderAddLight.Visibility = Visibility.Hidden;
                    ItemSetupBorder.Visibility = Visibility.Visible;
                    nozmalizeItemSetup();

                    newItemName.Text = Rooms[ActiveRoomIndex].Items[index].Name;
                    newItemType.SelectedIndex = (int)Rooms[ActiveRoomIndex].Items[index].Type;
                    newItemMqttToppic.Text = Rooms[ActiveRoomIndex].Items[index].MqttTopic;
                    newItemMqttOk.Text = Rooms[ActiveRoomIndex].Items[index].CommandON;
                    newItemMqttOff.Text = Rooms[ActiveRoomIndex].Items[index].CommandOFF;
                    newItemMqttRead.Text = Rooms[ActiveRoomIndex].Items[index].CommandRead;
                    break;

                //if settings mode is not active -> after click send command to topic related to item
                case Visibility.Hidden:
                    ActiveRoom.DisplaySumaryItemVals(sidePanel_room);


                    string json = JsonConvert.SerializeObject(exportRoom(Rooms[ActiveRoomIndex]));

                    string payload = ((Item)sender).Command;
                    string topic = ((Item)sender).MqttTopic;

                    try
                    {
                        await SmartHomeApp.MainWindow.MQTT_Publish(topic, payload);
                    }
                    catch (Exception)
                    {

                        throw;
                    }

                    break;
                default:
                    break;
            }
        }
        #endregion

        /// <summary>
        /// Method used for Room panel overview -> sum all lights and all active lights
        ///                                     -> sum all sockets
        /// </summary>
        private void DisplaySumaryRoomsVals()
        {
            int activeLightCount = 0;
            int lightCount = 0;
            int activeSocketCount = 0;
            int socketCount = 0;
            for (int i = 0; i < Rooms.Count; i++)
            {
                var summary = Rooms[i].DisplaySumaryItemVals(sidePanel_room);

                activeLightCount += summary.Item1.Item1;
                lightCount += summary.Item1.Item2;
                activeSocketCount += summary.Item2.Item1;
                socketCount += summary.Item2.Item2;
            }

            //display values to sidepanel
            LightCount = lightCount;
            SocketCount = socketCount;

            ActiveLightCount = activeLightCount;
            ActiveSocketCount = activeSocketCount;

        }

        #region Show/Hide items of room
        int light_column = 0;
        int socket_column = 0;
        int extra_column = 0;

        /// <summary>
        /// Method display all items of selected room into item grid
        /// </summary>
        /// <param name="index">index of room that will be displayed</param>
        private void displayItemsOfRoom(int index)
        {
            Rooms[index].LightCount = 0;
            Rooms[index].SocketCount = 0;
            Rooms[index].ExtraCount = 0;

            for (int i = 0; i < Rooms[index].Items.Count; i++)
            {
                if (Rooms[index].Items[i].Type == ItemType.Light)
                {
                    Rooms[index].Items[i].SetValue(Grid.ColumnProperty, light_column++);
                    Rooms[index].Items[i].SetValue(Grid.RowProperty, 1);
                    itemGrid.Children.Add(Rooms[index].Items[i]);
                    Rooms[index].LightCount++;
                }

                else if (Rooms[index].Items[i].Type == ItemType.Socket)
                {
                    Rooms[index].Items[i].SetValue(Grid.ColumnProperty, socket_column++);
                    Rooms[index].Items[i].SetValue(Grid.RowProperty, 3);
                    itemGrid.Children.Add(Rooms[index].Items[i]);
                    Rooms[index].SocketCount++;
                }
                else
                {
                    Rooms[index].Items[i].SetValue(Grid.ColumnProperty, extra_column++);
                    Rooms[index].Items[i].SetValue(Grid.RowProperty, 5);
                    itemGrid.Children.Add(Rooms[index].Items[i]);
                    Rooms[index].ExtraCount++;
                }
            }
        }
        /// <summary>
        /// Method remove all items of selected room from item grid
        /// </summary>
        /// <param name="index">index of room whos items has been displayed before</param>
        private void hideItemsOfRoom(int index)
        {
            for (int i = 0; i < Rooms[index].Items.Count; i++)
                itemGrid.Children.Remove(Rooms[index].Items[i]);

            light_column = 0;
            socket_column = 0;
            extra_column = 0;
        }
        #endregion

        bool roomsWasChaned = false;

        #region Add/remove room

        int MAX_ROOM_IN_ROW = 3;
        int MAX_ROOM_IN_COLUMN = 3;
        int currentColumn = 0;
        /// <summary>
        /// Method that create new instance of usercontrol Room if it is alowed, if not user is notified
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateNewRoom_MouseDown(object sender, RoutedEventArgs e)
        {
            if (Rooms.Count + 1 > MAX_ROOM_IN_ROW * MAX_ROOM_IN_COLUMN)
            {
                MessageBox.Show("Max room count", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            int newRoomNumber = (Rooms.Count > 0) ? Rooms[Rooms.Count - 1].Index + 1 : 0;

            Rooms.Add(new Room(newRoomNumber));
            Rooms[Rooms.Count - 1].Delete += deleteRoom_ProcessCompleted;
            Rooms[Rooms.Count - 1].Enter += enterRoom_ProcessCompleted;
            Rooms[Rooms.Count - 1].EnableDelete();

            InsertRoomToGrid(Rooms[Rooms.Count - 1]);
            //change was noted -> there is something to change
            roomsWasChaned = true;
        }


        /// <summary>
        /// Method that correctly destroy instance of Room  -> remove from grid
        ///                                                 -> remove events of click
        ///                                                 -> remove from Rooms list
        /// </summary>
        /// <param name="index"></param>
        void DeleteRoom(int index)
        {
            roomGrid.Children.Clear();
            roomGrid.RowDefinitions.Clear();
            roomGrid.ColumnDefinitions.Clear();

            Rooms[index].Delete -= deleteRoom_ProcessCompleted;
            Rooms[index].Enter -= enterRoom_ProcessCompleted;
            Rooms.Remove(Rooms[index]);
            currentColumn = 0;

            //reload of room grid
            for (int i = 0; i < Rooms.Count; i++)
                InsertRoomToGrid(Rooms[i]);
            //change was noted -> there is something to change
            roomsWasChaned = true;
        }
        #endregion

        #region Settings/Save/Discard
        /// <summary>
        /// method set mode of program to settings mode -> enable add new room 
        ///                                             -> enable delete option to room
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CogBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            for (int i = 0; i < Rooms.Count; i++)
                Rooms[i].EnableDelete();

            DisplaySidePanel(sidePanel_settings);
        }


        /// <summary>
        /// method save new room layout if there are changes to save    -> new "current_setup.json" file is created 
        ///                                                             -> rooms setup are sent through mqtt
        ///                                                             -> disable delete button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BorderSave_MouseDown(object sender, MouseButtonEventArgs e)
        {

            DisplaySidePanel(sidePanel_overview);
            SaveRoutine();
        }

        private async void SaveRoutine()
        {
            for (int i = 0; i < Rooms.Count; i++)
                Rooms[i].DisableDelete();

            DisplaySumaryRoomsVals();

            string topic = MainTopicThred;

            try
            {
                await SmartHomeApp.MainWindow.MQTT_Publish(topic, "start");
            }
            catch (Exception)
            { throw; }

            for (int i = 0; i < Rooms.Count; i++)
            {
                string new_json_string = JsonConvert.SerializeObject(exportRoom(Rooms[i]));
                try
                {
                    await SmartHomeApp.MainWindow.MQTT_Publish(topic, new_json_string);
                }
                catch (Exception)
                { throw; }
            }
            try
            {
                await SmartHomeApp.MainWindow.MQTT_Publish(topic, "end");
            }
            catch (Exception)
            { throw; }

            if (roomsWasChaned == true)
            {
                roomsWasChaned = false;
                string json = JsonConvert.SerializeObject(exportRooms(Rooms), Formatting.Indented);
                File.WriteAllText(@"current_setup.json", json);
            }
        }


        /// <summary>
        /// method that discard changes made on rooms   -> disable delete button
        ///                                             -> if there are changes, restore rooms from "current_setup.json" file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BorderDiscard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            for (int i = 0; i < Rooms.Count; i++)
                Rooms[i].DisableDelete();

            DisplaySidePanel(sidePanel_overview);

            if (roomsWasChaned == true)
            {
                roomsWasChaned = false;
                loadRooms();
            }
        }
        #endregion

        #region Export function (to JSON simplified classes)

        /// <summary>
        /// Method that returns simplified JSON format of class House
        /// </summary>
        /// <param name="data">input list of rooms to export</param>
        /// <returns>House class formated to export</returns>
        private ExportClasses.JsonHouse exportRooms(List<Room> data)
        {
            ExportClasses.JsonHouse export = new ExportClasses.JsonHouse();

            //export single room and add result to export list
            for (int i = 0; i < data.Count; i++)
                export.rooms.Add(exportRoom(data[i]));

            return export;
        }
        /// <summary>
        /// Method that returns simplified JSON format of class Room
        /// </summary>
        /// <param name="data">input Room for export</param>
        /// <returns>Room class formated to export</returns>
        private ExportClasses.JsonRoom exportRoom(Room data)
        {
            return new ExportClasses.JsonRoom(data);
        }
        /// <summary>
        /// Method that returns simplified JSON format of class Item
        /// </summary>
        /// <param name="data">Room contains item</param>
        /// <param name="index">index of item to export</param>
        /// <returns>Item class formated to export</returns>
        private ExportClasses.JsonItem exportItem(Room data, int index)
        {
            return new ExportClasses.JsonItem(data.Items[index]);
        }
        #endregion

        #region Cog/Save/Discard/Exit  - rooms

        /// <summary>
        /// Exit from currently displayed room, hide items, and show Room panel
        /// reset active room and active room index
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exit_border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DisplayMainPanel(mainPanel);
            DisplaySidePanel(sidePanel_overview);

            ActiveRoom.DisplaySumaryItemVals(sidePanel_room);
            DisplaySumaryRoomsVals();



            hideItemsOfRoom(ActiveRoomIndex);
            ActiveRoomIndex = -1;
            ActiveRoom = null;
        }

        /// <summary>
        /// Enable delete button to all items, hide infopanel and show settings panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CogBorder_room_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DisplaySidePanel(sidePanel_room_settings);
            RoomName_room_settings.Text = ActiveRoom.RoomName;
            RoomName_room_settings.IsEnabled = true;
            for (int i = 0; i < ActiveRoom.Items.Count; i++)
            {
                ActiveRoom.Items[i].EnableDelete();
                ActiveRoom.Items[i].NameLabel.Focusable = true;
            }
        }

        private async void BorderSave_room_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int error = 0;
            switch (ItemSetupBorder.Visibility)
            {
                case Visibility.Visible:
                    error = createNewItem();
                    if (error == 0)
                    {
                        ItemSetupBorder.Visibility = Visibility.Hidden;
                        ((TextBlock)BorderSave_room.Child).Text = "SAVE";
                        BorderAddLight.Visibility = Visibility.Visible;
                        nozmalizeItemSetup();
                    }


                    break;
                case Visibility.Hidden:
                    RoomName_room_settings.IsEnabled = false;
                    ActiveRoom.RoomName = RoomName_room_settings.Text;
                    DisplaySidePanel(sidePanel_room);
                    for (int i = 0; i < ActiveRoom.Items.Count; i++)
                    {
                        ActiveRoom.Items[i].DisableDelete();
                        ActiveRoom.Items[i].NameLabel.Focusable = false;
                        ActiveRoom.Items[i].Name = ActiveRoom.Items[i].NameLabel.Text;
                    }

                    break;
                default:

                    break;

            }

            if (error == 0)
            {
                string json = JsonConvert.SerializeObject(exportRooms(Rooms), Formatting.Indented);
                File.WriteAllText(@"current_setup.json", json);
                ActiveRoom.DisplaySumaryItemVals(sidePanel_room);

                ActiveRoom.DisplaySumaryItemVals(sidePanel_room);

                string payload = JsonConvert.SerializeObject(exportRoom(ActiveRoom));
                string topic = MainTopicThred + "/" + ActiveRoom.RoomName;
                try
                {
                    await SmartHomeApp.MainWindow.MQTT_Publish(topic, "start");
                    await SmartHomeApp.MainWindow.MQTT_Publish(topic, payload);
                    await SmartHomeApp.MainWindow.MQTT_Publish(topic, "end");
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }

        private void BorderDiscard_room_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (ItemSetupBorder.Visibility)
            {
                //if user tryed to add or change item -> scratch changes and hide input form
                case Visibility.Visible:
                    ItemSetupBorder.Visibility = Visibility.Hidden;
                    ((TextBlock)BorderSave_room.Child).Text = "SAVE";
                    BorderAddLight.Visibility = Visibility.Visible;
                    nozmalizeItemSetup();
                    break;

                //else 
                case Visibility.Hidden:
                    RoomName_room_settings.IsEnabled = false;
                    DisplaySidePanel(sidePanel_room);
                    for (int i = 0; i < ActiveRoom.Items.Count; i++)
                    {
                        ActiveRoom.Items[i].DisableDelete();
                        ActiveRoom.Items[i].NameLabel.Text = ActiveRoom.Items[i].Name;
                    }
                    //jak zajistit nezmenu?############################################################################################################
                    ActiveRoom.DisplaySumaryItemVals(sidePanel_room);
                    //DisplaySumaryRoomsVals();
                    break;
                default:

                    break;

            }
        }
        #endregion

        /// <summary>
        /// Find item from topic and aply data to this specific item
        /// </summary>
        /// <param name="topic">topic that should be related to one of items</param>
        /// <param name="data">payload comming from server (not unified format -> may create error</param>
        public void ApplyDataFromServer(string topic, string data)
        {
            //HMI connected -> send current layout to HMI panel
            if (topic == MainTopicThred && data == "hmi_connected")
            {
                this.Dispatcher.Invoke((new Action(() =>
                {
                    SaveRoutine();
                })));
                return;
            }

            //topic related to item -> find item and apply payload
            for (int i = 0; i < Rooms.Count; i++)
            {
                for (int j = 0; j < Rooms[i].Items.Count; j++)
                {
                    if (Rooms[i].Items[j].MqttTopic == topic)
                    {
                        if (data == Rooms[i].Items[j].CommandOFF)
                            Rooms[i].Items[j].Val = 0;
                        else if (data == Rooms[i].Items[j].CommandON)
                            Rooms[i].Items[j].Val = 1;
                        else
                        {
                            //number incoming -> try parse into double, if ok set value, if not notify user
                            double readVal = 0;
                            if (double.TryParse(data, out readVal) == true)
                                Rooms[i].Items[j].Val = readVal;
                            else
                                MessageBox.Show("Item: " + Rooms[i].Items[j].Name + " in room: " + Rooms[i].RoomName + " accepted unknown command\n\n Incoming command: " + data, "MQTT Command Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
        }



        #region CreateNewItems_MouseDown

        const int MAX_ITEM_COUNT = 5;

        /// <summary>
        /// macro that set applicaton to insert new item mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BorderAddLight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            nozmalizeItemSetup();                               //clear input form
            ItemSetupBorder.Visibility = Visibility.Visible;    //display input form
            ((TextBlock)BorderSave_room.Child).Text = "ADD";    //change function of save button
            BorderAddLight.Visibility = Visibility.Hidden;      //hide add button
        }

        /// <summary>
        /// Method that handle adding new item into active room
        /// </summary>
        /// <returns>   0-item correctly added
        ///             10-light cannot be add
        ///             20-socket cannot be add
        ///             30-extra cannot be add
        ///             </returns>
        private int createNewItem()
        {
            int error = checkParameters();      //if some of input parameters is missing, default parameters is set and user is notified about that
            if (error != 0)
                MessageBox.Show("Missing parameters set to default value", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

            switch ((ItemType)newItemType.SelectedIndex)
            {
                case ItemType.Light:
                    if (ActiveRoom.LightCount >= MAX_ITEM_COUNT)
                    {
                        MessageBox.Show("No more light cant be add", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return 10;
                    }
                    break;
                case ItemType.Socket:
                    if (ActiveRoom.SocketCount >= MAX_ITEM_COUNT)
                    {
                        MessageBox.Show("No more Socket cant be add", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return 20;
                    }
                    break;
                default:
                    if (ActiveRoom.ExtraCount >= MAX_ITEM_COUNT)
                    {
                        MessageBox.Show("No more Extra cant be add", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return 30;
                    }
                    break;
            }

            //new item is created and parameters are set from form
            Item newItem = new Item(ActiveRoom.Items.Count, (ItemType)newItemType.SelectedIndex);
            newItem.Name = newItemName.Text;
            newItem.MqttTopic = newItemMqttToppic.Text;
            newItem.CommandON = newItemMqttOk.Text;
            newItem.CommandOFF = newItemMqttOff.Text;
            newItem.CommandRead = newItemMqttRead.Text;
            newItem.Enter += enterItem_ProcessCompleted;
            newItem.Delete += deleteItem_ProcessCompleted;
            InsertItemToGrid(newItem);
            ActiveRoom.Items.Add(newItem);

            MainWindow.MQTT_Subscribe(newItem.MqttTopic);


            return 0;
        }
        /// <summary>
        /// Method will check input parameters for new item -> if something is missing default value will be set
        /// </summary>
        /// <returns>number od missing parameters is returned</returns>
        private int checkParameters()
        {
            int error = 0;
            //type wasnt selected -> avalible type was set automaticly
            if (newItemType.SelectedIndex == -1)
            {
                if (string.IsNullOrEmpty(newItemName.Text))
                {
                    if (ActiveRoom.LightCount < MAX_ITEM_COUNT)
                    {
                        newItemName.Text = "LIGHT" + ActiveRoom.LightCount;
                        newItemType.SelectedIndex = (int)ItemType.Light;
                    }
                    else if (ActiveRoom.SocketCount < MAX_ITEM_COUNT)
                    {
                        newItemName.Text = "SOCKET" + ActiveRoom.SocketCount;
                        newItemType.SelectedIndex = (int)ItemType.Light;
                    }
                    else if (ActiveRoom.ExtraCount < MAX_ITEM_COUNT)
                    {
                        newItemName.Text = "EXTRA" + ActiveRoom.ExtraCount;
                        newItemType.SelectedIndex = (int)ItemType.Light;
                    }
                    error++;
                }
            }
            //type was selected -> name will be create based on type
            else
            {
                if (string.IsNullOrEmpty(newItemName.Text))
                {
                    switch ((ItemType)newItemType.SelectedIndex)
                    {
                        case ItemType.Light:
                            newItemName.Text = "LIGHT" + ActiveRoom.LightCount;
                            break;
                        case ItemType.Socket:
                            newItemName.Text = "SOCKET" + ActiveRoom.SocketCount;
                            break;
                        case ItemType.Temp:
                            newItemName.Text = "TEMP" + ActiveRoom.ExtraCount;
                            break;
                        case ItemType.Humi:
                            newItemName.Text = "HUMI" + ActiveRoom.ExtraCount;
                            break;
                        case ItemType.CO2:
                            newItemName.Text = "CO2" + ActiveRoom.ExtraCount;
                            break;
                        case ItemType.Unknow:
                            newItemName.Text = "UNKNOW" + ActiveRoom.ExtraCount;
                            break;
                        default:
                            break;
                    }
                    error++;
                }
            }
            //MQTT topic is empty
            if (string.IsNullOrEmpty(newItemMqttToppic.Text))
            {
                newItemMqttToppic.Text = MainTopicThred + "/" + Rooms[ActiveRoomIndex].RoomName + "/" + newItemName.Text;
                error++;
            }
            //ON command is empty
            if (string.IsNullOrEmpty(newItemMqttOk.Text))
            {
                newItemMqttOk.Text = "1";
                error++;
            }
            //OFF command is empty
            if (string.IsNullOrEmpty(newItemMqttOff.Text))
            {
                newItemMqttOff.Text = "0";
                error++;
            }
            //READ command is empty
            if (string.IsNullOrEmpty(newItemMqttRead.Text))
            {
                newItemMqttRead.Text = "r";
                error++;
            }

            //if something is missing -> error > 0
            return error;

        }
        /// <summary>
        /// Method that reset values in newItem form
        /// </summary>
        private void nozmalizeItemSetup()
        {
            //var converter = new System.Windows.Media.BrushConverter();
            //Brush normalColor = (Brush)converter.ConvertFromString("#A39C9C");
            //newItemName.BorderBrush = normalColor;
            //newItemName.Foreground = normalColor;
            //newItemNameLabel.Foreground = normalColor;
            //newItemType.BorderBrush = normalColor;
            //newItemTypeLabel.Foreground = normalColor;
            //newItemMqttToppic.BorderBrush = normalColor;
            //newItemMqttToppicLabel.Foreground = normalColor;
            //newItemMqttOk.BorderBrush = normalColor;
            //newItemMqttOff.BorderBrush = normalColor;
            //newItemMqttRead.BorderBrush = normalColor;

            newItemName.Text = "";
            newItemType.SelectedIndex = -1;
            newItemMqttToppic.Text = "";
            newItemMqttOk.Text = "";
            newItemMqttOff.Text = "";
            newItemMqttRead.Text = "";
        }

        #endregion

        #region InsertToGrid
        /// <summary>
        /// Method to set and display culomn and row for item from argument in currently displayed item grid
        /// </summary>
        /// <param name="itemToInsert">Item will be display in correct position</param>
        private void InsertItemToGrid(Item itemToInsert)
        {
            switch (itemToInsert.Type)
            {
                case ItemType.Light:
                    itemToInsert.SetValue(Grid.ColumnProperty, ActiveRoom.LightCount++);
                    itemToInsert.SetValue(Grid.RowProperty, 1);
                    break;
                case ItemType.Socket:
                    itemToInsert.SetValue(Grid.ColumnProperty, ActiveRoom.SocketCount++);
                    itemToInsert.SetValue(Grid.RowProperty, 3);
                    break;
                default:
                    itemToInsert.SetValue(Grid.ColumnProperty, ActiveRoom.ExtraCount++);
                    itemToInsert.SetValue(Grid.RowProperty, 5);
                    break;
            }

            itemGrid.Children.Add(itemToInsert);
        }

        /// <summary>
        /// Method to set and display culomn and row for new room from argument in main room grid
        /// </summary>
        /// <param name="roomToAdd">Room will be display in correct position</param>
        void InsertRoomToGrid(Room roomToAdd)
        {
            //int ROOM_IN_ROW = (Rooms.Count < 5) ? 2 : 3;
            if (roomGrid.ColumnDefinitions.Count < MAX_ROOM_IN_ROW)
            {
                var newCollumn = new ColumnDefinition();
                newCollumn.Width = new GridLength(1, GridUnitType.Star);
                roomGrid.ColumnDefinitions.Add(newCollumn);
                currentColumn = roomGrid.ColumnDefinitions.Count - 1;
            }
            else
                currentColumn++;

            if ((currentColumn % MAX_ROOM_IN_ROW) == 0)
            {
                var newRow = new RowDefinition();
                newRow.Height = new GridLength(1, GridUnitType.Star);
                roomGrid.RowDefinitions.Add(newRow);
                currentColumn = 0;
            }

            roomToAdd.SetValue(Grid.ColumnProperty, currentColumn);
            roomToAdd.SetValue(Grid.RowProperty, roomGrid.RowDefinitions.Count - 1);
            roomGrid.Children.Add(roomToAdd);
        }
        #endregion

        #region Display Side-MainPanel
        /// <summary>
        /// Macro to display side panel from argument
        /// </summary>
        /// <param name="SidePanelToDisplay">side panel to display</param>
        private void DisplaySidePanel(Grid SidePanelToDisplay)
        {
            sidePanel_settings.Visibility = Visibility.Hidden;
            sidePanel_overview.Visibility = Visibility.Hidden;
            sidePanel_room.Visibility = Visibility.Hidden;
            sidePanel_room_settings.Visibility = Visibility.Hidden;

            SidePanelToDisplay.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Macro to display mainpanel from argument
        /// </summary>
        /// <param name="MainPanelToDisplay">main panel to display</param>
        private void DisplayMainPanel(Border MainPanelToDisplay)
        {
            mainPanel.Visibility = Visibility.Hidden;
            roomPanel.Visibility = Visibility.Hidden;

            MainPanelToDisplay.Visibility = Visibility.Visible;
        }
        #endregion

    }
}
