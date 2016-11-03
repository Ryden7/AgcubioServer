using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace AgCubio
{
    public static class Server
    {
        public static World world;

        public static LinkedList<PreservedState> connections = new LinkedList<PreservedState>();

        private static DateTime start_time;

        private static void Main(string[] args)
        {
            world = new World();
            start();
            Console.Read();
        }

        public static void start()
        {
            Timer timer = new Timer();
            timer.Interval = 1.0 / (double)Server.world.hps * 1000.0;
            timer.Elapsed += new ElapsedEventHandler(Server.update);
            timer.Start();
            Cube cube;
            while (Server.world.food(1.0, out cube))
            {
            }
            Network.Server_Awaiting_Client_Loop(new Action<PreservedState>(Server.handleClient));
            Server.start_time = DateTime.Now;
        }

        public static void handleClient(PreservedState state)
        {
            state.callback = new Action<PreservedState>(Server.receivePlayer);
            Network.i_want_more_data(state);
        }

        public static void receivePlayer(PreservedState state)
        {
            Socket workSocket = state.socket;
            string name = state.sb.ToString();
            //string text = Regex.Replace(state.sb.ToString().Trim(), "\\n|\\t|\\r", "");

            state.callback = new Action<PreservedState>(Server.handleData);
            Cube cube = Server.world.add_player(name);
            state.uid = cube.uid;

            lock (Server.world)
            {
                Network.Send(workSocket, JsonConvert.SerializeObject(cube) + "\n");
                StringBuilder stringBuilder = new StringBuilder();
                foreach (Cube current in Server.world.foods.Values)
                {
                    stringBuilder.Append(JsonConvert.SerializeObject(current) + "\n");
                }
                Network.Send(workSocket, stringBuilder.ToString());
            }

            lock (Server.connections)
            {
                Server.connections.AddLast(state);
            }
            Network.i_want_more_data(state);
        }

        private static void handleData(PreservedState state)
        {
            try
            {
                char[] separator = new char[]
                {
                    '\n'
                };
                string[] array = state.sb.ToString().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                int num = 0;
                int num2 = 0;
                bool flag = false;
                for (int i = 0; i < array.Length; i++)
                {
                    string text = array[i];
                    if (text.Length > 1 && text[0] == '(' && text[text.Length - 1] == ')')
                    {
                        string[] array2 = text.Substring(1, text.Length - 2).Split(new char[]
                        {
                            ','
                        });
                        if (array2[0] == "move")
                        {
                            num = int.Parse(array2[1]);
                            num2 = int.Parse(array2[2]);
                        }
                        else if (array2[0] == "split")
                        {
                            num = int.Parse(array2[1]);
                            num2 = int.Parse(array2[2]);
                            flag = true;
                            break;
                        }
                    }
                }
                lock (Server.world)
                {
                    long uid = state.uid;
                    if (flag)
                    {
                        Server.world.split(uid, num, num2);
                    }
                    else
                    {
                        Server.world.movePlayer(uid, num, num2);
                    }
                }
                state.sb.Clear();
                Network.i_want_more_data(state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void update(object sender, ElapsedEventArgs e)
        {
            ((Timer)sender).Stop();
            Server.world.heartbeats++;
            if (Server.world.heartbeats % 30 == 0)
            {
                double arg_48_0 = DateTime.Now.Subtract(Server.start_time).TotalSeconds;
            }
            Cube cube;
            Server.world.food(0.25, out cube);
            if (cube != null)
            {
                lock (Server.connections)
                {
                    LinkedListNode<PreservedState> linkedListNode = Server.connections.First;
                    while (linkedListNode != null)
                    {
                        PreservedState value = linkedListNode.Value;

                        // modified network.send
                        if (!Network.Send(value.socket, JsonConvert.SerializeObject(cube) + "\n"))
                        {
                            LinkedListNode<PreservedState> next = linkedListNode.Next;
                            Server.connections.Remove(linkedListNode);
                            linkedListNode = next;
                        }
                        else
                        {
                            linkedListNode = linkedListNode.Next;
                        }
                    }
                }
            }
            LinkedList<Cube> linkedList = Server.world.eatFood();
            lock (Server.connections)
            {
                foreach (Cube current in linkedList)
                {
                    foreach (PreservedState current2 in Server.connections)
                    {
                        Network.Send(current2.socket, JsonConvert.SerializeObject(current) + "\n");
                    }
                }
            }
            Server.world.attrition();
            LinkedList<Cube> linkedList2 = Server.world.eat();
            lock (Server.connections)
            {
                lock (Server.world)
                {
                    foreach (Cube current3 in linkedList2)
                    {
                        foreach (PreservedState current4 in Server.connections)
                        {
                            Network.Send(current4.socket, JsonConvert.SerializeObject(current3) + "\n");
                        }
                    }
                    foreach (Cube current5 in Server.world.players.Values)
                    {
                        foreach (PreservedState current6 in Server.connections)
                        {
                            Network.Send(current6.socket, JsonConvert.SerializeObject(current5) + "\n");
                        }
                    }
                }
            }
            ((Timer)sender).Start();
        }
    }
}