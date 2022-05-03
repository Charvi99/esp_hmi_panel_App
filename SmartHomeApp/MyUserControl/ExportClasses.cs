using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeApp.MyUserControl
{
    public class ExportClasses
    {
        public class JsonHouse
        {

            public int roomsCount { get { return rooms.Count; } }
            public List<JsonRoom> rooms;
            public JsonHouse()
            {
                rooms = new List<JsonRoom>();
            }
        }

        public class JsonRoom
        {
            public int itemCount { get { return items.Count; } }
            public List<JsonItem> items;
            public string name;
            public int lightCount;
            public int socketCount;
            public int extraCount;
            public int index;
            public JsonRoom(Room input)
            {
                if (input != null)
                {
                    name = input.RoomName;
                    index = input.Index;
                    lightCount = input.LightCount;
                    socketCount = input.SocketCount;
                    extraCount = input.ExtraCount;
                    items = new List<JsonItem>();

                    for (int i = 0; i < input?.Items.Count; i++)
                    {
                        items.Add(new JsonItem(input?.Items[i]));
                    }

                }
            }
        }

        public class JsonItem
        {
            public ItemType typ;
            public string nam;
            public double val;
            public string sVal  ;
            public string top;
            public string on;
            public string off;
            public string red;

            public JsonItem(Item input)
            {
                if(input != null)
                {
                    typ = input.Type;
                    nam = input.Name;
                    val = input.Val;
                    sVal = input.ValString;
                    top = input.MqttTopic;
                    on = input.CommandON;
                    off = input.CommandOFF;
                    red = input.CommandRead;
                }

            }
        }

    }
}
