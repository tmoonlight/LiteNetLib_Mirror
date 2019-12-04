using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LockStepClient
{
    class Program
    {
        static Random rand = new Random(996);
        private static NetPeer myPeer;
        private static NetDataWriter myNetDataWriter;
        private static string redkey = "";
        static void Main(string[] args)
        {
            myNetDataWriter = new NetDataWriter();
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager client = new NetManager(listener);
            client.Start();
            client.Connect("localhost" /* host ip or name */, 9050 /* port */, "SomeConnectionKey" /* text key or NetDataWriter */);

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

            Task.Run(readkey);
            while (true)
            {
                client.PollEvents();
                const int alTime = 1000;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Console.WriteLine(rand.Next());
                Console.Write("----");
                Console.WriteLine(redkey);
                myNetDataWriter.Reset();
                myNetDataWriter.Put(redkey);
                myPeer?.Send(myNetDataWriter, DeliveryMethod.ReliableOrdered);
                sw.Stop();
                Thread.Sleep(Math.Clamp(alTime - (int)sw.ElapsedMilliseconds, 0, alTime));
            }

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
