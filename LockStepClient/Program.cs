using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LockStepClient
{
    //队列

    //单个操作
    public class SyncData:INetSerializable
    {
        short angleXLeft = 180;
        short angleXRight = 180;
        byte ski = 0b00001111;
        private int size = 2 + 2 + 1;


        public void Serialize(NetDataWriter writer)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(NetDataReader reader)
        {
            throw new NotImplementedException();
        }
    }

    class Program
    {
        static Random rand = new Random(996);
        private static NetPeer myPeer;//对端
        private static NetDataWriter myNetDataWriter;
        private static NetPacketProcessor _packetProcessor; //数据包区分

        private static string redkey = "";
        static void Main(string[] args)
        {
            myNetDataWriter = new NetDataWriter();
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager client = new NetManager(listener); //自己
            client.Start();
            client.Connect("localhost" /* host ip or name */, 9050 /* port */, "SomeConnectionKey" /* text key or NetDataWriter */);
            //_packetProcessor.SubscribeReusable<SyncData>(OnReceiveSyncData);

            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                Console.WriteLine("We got: {0}", dataReader.GetString(100 /* max length of string */));
                dataReader.Recycle();
            };
            listener.PeerConnectedEvent += peer => { myPeer = peer; };

            ////轮询才能触发事件
            //while (!Console.KeyAvailable)
            //{
            //    client.PollEvents();
            //    Thread.Sleep(15);
            //}

            //client.Stop();
            var i = 0;
            Task.Run(readkey);
            while (true)
            {
                client.PollEvents();
                const int alTime = 15;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Console.WriteLine(rand.Next());
                Console.Write("----");
                Console.WriteLine(redkey);
                myNetDataWriter.Reset();
                myNetDataWriter.Put(i.ToString());
                myPeer?.Send(myNetDataWriter, DeliveryMethod.ReliableOrdered);
                sw.Stop();
                Thread.Sleep(Math.Clamp(alTime - (int)sw.ElapsedMilliseconds, 0, alTime));
                i++;
            }

        }

        private static void OnReceiveSyncData(SyncData data)
        {
            //加入队列
            //throw new NotImplementedException();
        }

        static void readkey()
        {
            while (true)
            {
                redkey = Console.ReadKey().KeyChar.ToString();
            }
        }
    }
}
