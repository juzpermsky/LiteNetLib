using System;
using System.Threading;
using LiteNetLib;

namespace SpeedTest
{
    class Program
    {
        private static int sampleCount = 10000;
        private static DateTime firstSent;
        private static byte[] sample = new byte[100];
        private static DateTime lastSent;
        private static int svUpdateTime = 0;
        private static int clUpdateTime = 0;


        static void ServerThread()
        {
            var svListener = new EventBasedNetListener();
            var rcv = 0;
            var firstReceived = DateTime.Now;
            svListener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine($"Server: Peer {peer.EndPoint} connected with id {peer.ConnectId}");
            };
            
            svListener.NetworkReceiveEvent += (peer, reader) =>
            {
                if (rcv == 0)
                {
                    firstReceived = DateTime.Now;
                    Console.WriteLine($"First received after {firstReceived - firstSent}");
                }

                rcv++;
                if (rcv == sampleCount)
                {
                    var lastReceived = DateTime.Now;
                    Console.WriteLine($"{sampleCount} samples received in {lastReceived - firstReceived}");
                    Console.WriteLine($"Last received after {lastReceived - lastSent}");
                    peer.NetManager.DisconnectPeer(peer);
                }
            };

            svListener.PeerDisconnectedEvent += (peer, info) =>
            {
                Console.WriteLine("Server: DisconnectEvent received");
                peer.NetManager.Stop();
            };

            var svMng = new NetManager(svListener, 10, "test");
            svMng.UpdateTime = svUpdateTime;
            svMng.Start(5000);
            while (svMng.IsRunning)
            {
                svMng.PollEvents();
            }
        }

        static void ClientThread()
        {
            var clListener = new EventBasedNetListener();
            clListener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine($"Client: Peer {peer.EndPoint} connected with id {peer.ConnectId}");
                for (var i = 0; i < sampleCount; i++)
                {
                    if (i == 0)
                    {
                        firstSent = DateTime.Now;
                    }
                    if (i == sampleCount-1)
                    {
                        lastSent = DateTime.Now;
                    }

                    peer.Send(sample, SendOptions.ReliableOrdered);
                }
            };
            clListener.PeerDisconnectedEvent += (peer, info) =>
            {
                Console.WriteLine("Client: DisconnectEvent received");
                peer.NetManager.Stop();
            };

            var clMng = new NetManager(clListener, 10, "test");
            clMng.UpdateTime = clUpdateTime;
            clMng.Start();
            clMng.Connect("localhost", 5000);

            while (clMng.IsRunning)
            {
                clMng.PollEvents();
            }
        }

        static void Main(string[] args)
        {
            var st = new Thread(ServerThread);
            var ct = new Thread(ClientThread);
            st.Start();
            ct.Start();
            st.Join();
            ct.Join();
        }
    }
}