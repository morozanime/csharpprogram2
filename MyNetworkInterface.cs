using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp2
{
    public class Sclient
    {
        public System.Net.IPAddress addr;
        public int alive;
        public bool selected;
        public Sclient(System.Net.IPAddress addr, int alive)
        {
            this.addr = addr;
            this.alive = alive;
            selected = false;
        }
        public bool dec(int a)
        {
            alive -= a;
            if (alive <= 0 && -alive < a)
                return true;
            else return false;
        }

        public void set(int a)
        {
            alive = a;
        }
    }

    public class MyNetworkInterface
    {
        private const int clientTimeout = 10;
        private byte[] magicHeader = Encoding.ASCII.GetBytes("ClippeR@");
        public Int32 clientID { get; }
        public Dictionary<Int32, Sclient> clients { get; }
        public List<System.Net.IPAddress> IPv4AddressList { get; }
        private Random rnd;
        private UdpClient udpClient;
        private const int udpPort = 9999;
        public event EventHandler newClient;
        public event EventHandler newDatagram;
        private byte[] datagram;
        public MyNetworkInterface()
        {
            IPv4AddressList = new List<System.Net.IPAddress>();
            rnd = new Random((int)DateTime.Now.ToBinary());
            clientID = rnd.Next();
            refreshIPv4AddressList();

            clients = new Dictionary<Int32, Sclient>();
            udpClient = new UdpClient();
            udpClient.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, udpPort));
            var from = new System.Net.IPEndPoint(0, 0);
            Task.Run(() =>
            {
                while (true)
                {
                    int pos = 0;
                    var recvBuffer = udpClient.Receive(ref from);
                    if (recvBuffer.Length < magicHeader.Length + 4)
                        continue;
                    if (!recvBuffer.Skip(pos).Take(magicHeader.Length).SequenceEqual(magicHeader))
                        continue;
                    pos += magicHeader.Length;
                    Int32 id = BitConverter.ToInt32(recvBuffer.Skip(pos).Take(4).ToArray(), 0);
                    pos += 4;
                    if (clients.ContainsKey(id))
                        clients[id].set(clientTimeout);
                    if (pos < recvBuffer.Length)
                    {
                        //packet with DATA
                        if (clients.ContainsKey(id) && clients[id].selected)
                        {
                            datagram = recvBuffer.Skip(pos).ToArray();
                            Console.WriteLine("New " + datagram.Length.ToString() + " bytes DATA from " + from.Address.ToString() + ":" + from.Port.ToString());
                            newDatagram?.Invoke(null, null);
                        }
                    }
                    else
                    {
                        //empty packet
                        if (!clients.ContainsKey(id) && id != clientID)
                        {
                            Console.WriteLine("New client " + from.Address.ToString() + ":" + from.Port.ToString());
                            clients[id] = new Sclient(from.Address, clientTimeout);
                            newClient?.Invoke(null, null);
                        }
                    }
                }
            });
            var timer = new System.Windows.Forms.Timer();
            timer.Tick += Timer_Tick;
            timer.Interval = 2000;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var sendbuf = makePkt(new byte[0]);
            udpClient.Send(sendbuf, sendbuf.Length, "255.255.255.255", udpPort);
            bool refreshNeed = false;
            foreach (var c in clients)
                if (c.Value.dec(2))
                    refreshNeed = true;
            if (refreshNeed)
                newClient?.Invoke(null, null);
        }

        public void setClientEnabled(Int32 clientID, bool enabled)
        {
            //            Console.WriteLine("ID " + clientID.ToString() + "--" + enabled.ToString());
            if (clients.ContainsKey(clientID))
            {
                Sclient s = clients[clientID];
                s.selected = enabled;
                clients[clientID] = s;
            }
            //            foreach (var c in clients)
            //            {
            //                Console.WriteLine("Id " + c.Key.ToString() + "--" + c.Value.selected.ToString());
            //            }
        }
        public byte[] getData()
        {
            return datagram;
        }

        private byte[] makePkt(byte[] data)
        {
            int pos = 0;
            var seralizedId = BitConverter.GetBytes(clientID);
            var pkt = new byte[magicHeader.Length + seralizedId.Length + data.Length];
            Buffer.BlockCopy(magicHeader, 0, pkt, pos, magicHeader.Length);
            pos += magicHeader.Length;
            Buffer.BlockCopy(seralizedId, 0, pkt, pos, seralizedId.Length);
            pos += seralizedId.Length;
            Buffer.BlockCopy(data, 0, pkt, pos, data.Length);
            pos += data.Length;
            return pkt;
        }
        public bool send(byte[] data)
        {
            var sendbuf = makePkt(data);
            foreach (Int32 id in clients.Keys)
            {
                if (clients[id].selected)
                {
                    int r = udpClient.Send(sendbuf, sendbuf.Length, new System.Net.IPEndPoint(clients[id].addr, udpPort));
                    //                    Console.WriteLine("Send " + clients[id].ToString() + " " + r.ToString() + " bytes " + ((sendbuf.Length == r) ? "OK" : "Error"));
                }
            }
            return true;
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
