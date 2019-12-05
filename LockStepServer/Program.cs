using System;
using System.Diagnostics;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LockStepServer
{
    class Program
    {
        static Random rand = new Random(996);
        static EventBasedNetListener listener;// = new EventBasedNetListener();
        static NetManager server;// = new NetManager(listener);
        static void Main(string[] args)
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.Start(9050 /* port */);

            listener.ConnectionRequestEvent += request =>
            {
                if (server.PeersCount < 10 /* max connections */)
                    request.AcceptIfKey("SomeConnectionKey");
                else
                    request.Reject();
            };

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("We got connection: {0}", peer.EndPoint); // Show peer ip
                NetDataWriter writer = new NetDataWriter();                 // Create writer class
                writer.Put("Hello client!");                                // Put some string
                peer.Send(writer, DeliveryMethod.ReliableOrdered);             // Send with reliability
            };
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
            int i = 0;
            while (true)
            {
                server.PollEvents(); //不会阻塞
                const int alTime = 1000;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Console.Write(i);
                Console.Write(":");
                Console.WriteLine(rand.Next());
                Console.Write("----");
                sw.Stop();
                Thread.Sleep(Math.Clamp(alTime - (int)sw.ElapsedMilliseconds, 0, alTime));
                i++;
            }

            ////轮询才能触发事件
            //while (!Console.KeyAvailable)
            //{
            //    server.PollEvents();
            //    Thread.Sleep(15);
            //}

            server.Stop();
        }

        private static void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {

            Console.Write("****===>");
            ; Console.WriteLine(reader.GetString());
            //throw new NotImplementedException();
        }
    }
}
