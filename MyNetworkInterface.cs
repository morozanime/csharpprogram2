using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp2
{
    public class MyNetworkInterface
    {
        public Dictionary<Int32, System.Net.IPAddress> clients { get; }
        public List<System.Net.IPAddress> IPv4AddressList { get; }
        private Random rnd;
        public Int32 clientID { get; }
        private UdpClient udpClient;
        private int udpPort;
        public event EventHandler newClient;
        public event EventHandler newDatagram;
        private byte[] datagram;
        public MyNetworkInterface()
        {
            IPv4AddressList = new List<System.Net.IPAddress>();
            rnd = new Random((int)DateTime.Now.ToBinary());
            clientID = rnd.Next();
            refreshIPv4AddressList();

            clients = new Dictionary<Int32, System.Net.IPAddress>();
            udpPort = 9999;
            udpClient = new UdpClient();
            udpClient.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, udpPort));
            var from = new System.Net.IPEndPoint(0, 0);
            Task.Run(() =>
            {
                while (true)
                {
                    var recvBuffer = udpClient.Receive(ref from);
                    if (recvBuffer.Length >= 12 && recvBuffer.Take(8).SequenceEqual(Encoding.ASCII.GetBytes("ClippeR@")))
                    {
                        int id = BitConverter.ToInt32(recvBuffer.Skip(8).Take(4).ToArray(), 0);
                        if (recvBuffer.Length == 12)
                        {
                            if (!clients.ContainsKey(id) && id != clientID)
                            {
                                Console.WriteLine("New client " + from.Address.ToString() + ":" + from.Port.ToString());
                                clients[id] = from.Address;
                                var handler = newClient;
                                if (handler != null)
                                {
                                    handler(null, null);
                                }
                            }
                        }
                        else
                        {
                            if (clients.ContainsKey(id))
                            {
                                datagram = recvBuffer.Skip(12).Take(recvBuffer.Length - 12).ToArray();
                                Console.WriteLine("New " + (recvBuffer.Length - 12).ToString() + " bytes DATA from " + from.Address.ToString() + ":" + from.Port.ToString());
                                var handler = newDatagram;
                                if (handler != null)
                                {
                                    handler(null, null);
                                }
                            }
                        }
                    }
                }
            });
            TimerCallback tm = new TimerCallback(udpPollAll);
            System.Threading.Timer timer = new System.Threading.Timer(tm, null, 0, 2000);
        }

        public byte[] getData()
        {
            return datagram;
        }
        public bool send(byte[] data)
        {
            Console.WriteLine("Send " + data.Length.ToString());
            var sendbuf = new byte[12 + data.Length];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("ClippeR@"), 0, sendbuf, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(clientID), 0, sendbuf, 8, 4);
            Buffer.BlockCopy(data, 0, sendbuf, 12, data.Length);
            foreach (Int32 id in clients.Keys)
            {
                udpClient.Send(sendbuf, sendbuf.Length, clients[id].ToString(), udpPort);
            }
            return true;
        }
        private void udpPollAll(object obj)
        {
            var sendbuf = new byte[12];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("ClippeR@"), 0, sendbuf, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(clientID), 0, sendbuf, 8, 4);
            udpClient.Send(sendbuf, sendbuf.Length, "255.255.255.255", udpPort);
        }
        public void refreshIPv4AddressList()
        {
            IPv4AddressList.Clear();
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
                foreach (UnicastIPAddressInformation ii in adapter.GetIPProperties().UnicastAddresses)
                    if (ii.Address.AddressFamily == AddressFamily.InterNetwork)
                        IPv4AddressList.Add(ii.Address);
        }
    }
}
