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
using MQTTnet;
using System.Security.Authentication;
using MQTTnet.Client;


namespace SmartHomeApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public enum ItemType
    {
        Light,
        Socket,
        Temp,
        Humi,
        CO2,
        Unknow
    }


    public partial class MainWindow : Window
    {

        public MyUserControl.House mainHouse { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            mainHouse = new MyUserControl.House();
            mainGrid.Children.Add(mainHouse);

            isReconnect = true;
            Task.Run(async () => { await ConnectMqttServerAsync(); });
            //Subscribe_Click(null, null);
        }

        public static IMqttClient mqttClient = null;
        private bool isReconnect = true;
        MQTTnet.Client.IMqttClientOptions options = null;

        static int count = 0;
        static public async Task MQTT_Publish(string topic, string payload)
        {
            if (string.IsNullOrEmpty(topic) || !mqttClient.IsConnected)
            {
                //MessageBox.Show("MQTT Error");
                return;
            }

            ///qos=0，-> jednou se odešle
            ///QoS 1: -> určite přijde alespon jednou
            ///QoS 2: -> nejvyssi uroven, zprava prijde určite pouze jednou

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithAtMostOnceQoS()
                .WithRetainFlag(true)
                .Build();

            await mqttClient.PublishAsync(message);
        }

        static public async Task MQTT_Subscribe(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show("Subscribe is Empty！");
                return;
            }

            if (!mqttClient.IsConnected)
            {
                MessageBox.Show("MQTT Client isn't connected！");
                return;
            }

            // Subscribe to a topic
            await mqttClient.SubscribeAsync(new TopicFilterBuilder()
                .WithTopic(topic)
                .WithAtMostOnceQoS()
                .Build()
                );


            //this.Dispatcher.Invoke((new Action(() =>
            //{
            //    txtReceiveMessage.AppendText($"Client is subsribeing: [{topic}]topic{Environment.NewLine}");
            //})));
        }
        static public async Task MQTT_Unubscribe(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show("Unubscribe is Empty！");
                return;
            }

            if (!mqttClient.IsConnected)
            {
                MessageBox.Show("MQTT Client isn't connected！");
                return;
            }

            // Unsubscribe to a topic

            await mqttClient.UnsubscribeAsync(topic);
            //this.Dispatcher.Invoke((new Action(() =>
            //{
            //    txtReceiveMessage.AppendText($"Client is subsribeing: [{topic}]topic{Environment.NewLine}");
            //})));
        }

        private async Task ConnectMqttServerAsync()
        {

            if (mqttClient == null)
            {
                var factory = new MqttFactory();
                mqttClient = factory.CreateMqttClient();

                mqttClient.ApplicationMessageReceived += MqttClient_ApplicationMessageReceived;
                mqttClient.Connected += MqttClient_Connected;
                mqttClient.Disconnected += MqttClient_Disconnected;
            }

            try
            {
                CreteClient();
                await mqttClient.ConnectAsync(options);

            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke((new Action(() =>
                {
                    txtReceiveMessage.AppendText("Failed to connect to MQTT server！" + Environment.NewLine + ex.Message + Environment.NewLine);
                })));
            }
    //        await mqttClient.SubscribeAsync(new TopicFilterBuilder()
    //                        .WithTopic("/house/test")
    //                        .WithAtMostOnceQoS()
    //                        .Build()
    //                        );
    //        await mqttClient.SubscribeAsync(new TopicFilterBuilder()
    //            .WithTopic("/topic/qos1")
    //            .WithAtMostOnceQoS()
    //            .Build()
    //            );
    //        await mqttClient.SubscribeAsync(new TopicFilterBuilder()
    //            .WithTopic("/topic/qos0")
    //            .WithAtMostOnceQoS()
    //            .Build()
    //);
        }
        private void CreteClient()
        {
            //getting inside actual thread
            //if (!Dispatcher.CheckAccess())
            //{
            //    // We're not in the UI thread, ask the dispatcher to call this same method in the UI thread, then exit
            //    Dispatcher.BeginInvoke(new Action(CreteClient));
            //    return;
            //}
            //Create TCP based options using the builder.
            var id = "HELLO";
            string ip = "mqtt.flespi.io";
            int port = 1883;
            string user = "sSDcvAHNgBUtCXHPbsToGKTncTgdZNFht6jOwRpBgaSGXplWF299BQmBztEo47vn";
            string pass = "";
            options = new MqttClientOptionsBuilder()
                        .WithClientId(id)
                        .WithTcpServer(ip, port)
                        .WithCredentials(user, pass)
                        .WithCleanSession()
                        .Build();

            id = "HELLO";
            ip = "mqtt.eclipseprojects.io";
            port = 1883;
            user = "";
            pass = "";
            options = new MqttClientOptionsBuilder()
                         .WithClientId(id)
                         .WithTcpServer(ip, port)
                         .WithCredentials(user, pass)
                         .WithCleanSession()
                         .Build();
            return;
        }

        private async void MqttClient_Connected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke((new Action(() =>
            {
                txtReceiveMessage.Clear();
                txtReceiveMessage.AppendText("Connected to MQTT server！" + Environment.NewLine);
            })));
            await MQTT_Subscribe(mainHouse.MainTopicThred);

            for (int i = 0; i < mainHouse.Rooms.Count; i++)
            {
                await SmartHomeApp.MainWindow.MQTT_Subscribe(mainHouse.MainTopicThred + "/" + mainHouse.Rooms[i].RoomName);
                for (int j = 0; j < mainHouse.Rooms[i].Items.Count; j++)
                {
                    await SmartHomeApp.MainWindow.MQTT_Subscribe(mainHouse.Rooms[i].Items[j].MqttTopic);
                }
            }
        }

        private void MqttClient_Disconnected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke((new Action(() =>
            {
                txtReceiveMessage.Clear();
                DateTime curTime = new DateTime();
                curTime = DateTime.UtcNow;
                txtReceiveMessage.AppendText($">> [{curTime.ToLongTimeString()}]");
                txtReceiveMessage.AppendText("MQTT disconnected！" + Environment.NewLine);
            })));

            //Reconnecting
            if (isReconnect)
            {
                this.Dispatcher.Invoke((new Action(() =>
                {
                    txtReceiveMessage.AppendText("Trying to reconnect.." + Environment.NewLine);
                })));

                CreteClient();
                this.Dispatcher.Invoke((new Action(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    try
                    {
                        await mqttClient.ConnectAsync(options);
                    }
                    catch
                    {
                        txtReceiveMessage.AppendText("### RECONNECTING FAILED ###" + Environment.NewLine);
                    }
                })));
            }
            else
            {
                this.Dispatcher.Invoke((new Action(() =>
                {
                    txtReceiveMessage.AppendText("Offline！" + Environment.NewLine);
                })));
            }
        }
        private void MqttClient_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            this.Dispatcher.Invoke((new Action(() =>
            {
                txtReceiveMessage.AppendText($">> {"### RECEIVED APPLICATION MESSAGE ###"}{Environment.NewLine}");
                txtReceiveMessage.AppendText($">> Topic = {e.ApplicationMessage.Topic}{Environment.NewLine}");
                txtReceiveMessage.AppendText($">> Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}{Environment.NewLine}");
                txtReceiveMessage.AppendText($">> QoS = {e.ApplicationMessage.QualityOfServiceLevel}{Environment.NewLine}");
                txtReceiveMessage.AppendText($">> Retain = {e.ApplicationMessage.Retain}{Environment.NewLine}");
                txtReceiveMessage.ScrollToEnd();
            })));

            mainHouse.ApplyDataFromServer(e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
        }

        private void btnLogIn_Click(object sender, EventArgs e)
        {
            isReconnect = true;
            Task.Run(async () => { await ConnectMqttServerAsync(); });
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            isReconnect = false;
            try
            {
                Task.Run(async () => { await mqttClient.DisconnectAsync(); });

            }
            catch (Exception)
            {
                MessageBox.Show("nejsi pripojen");
                throw;
            }
        }

    }
}
