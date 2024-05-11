using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ArtNet2Osc
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const int ArtNetPort = 6454;
            UdpClient udpClient = new UdpClient(ArtNetPort);

            OscSender oscSender = new OscSender("127.0.0.1", 3001);
            oscSender.Connect();

            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (true)
                {
                    byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                    if (IsValidArtNetPacket(receivedBytes, out int universe, out int dmxDataLength))
                    {
                        for (int channel = 1; channel <= dmxDataLength; channel++)
                        {
                            byte channelData = receivedBytes[17 + channel];
                            string oscAddressToSend = $"/universe/{channel}/{universe}";
                            oscSender.Send(oscAddressToSend, channelData.ToString("X2"));
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid Art-Net packet received from " + remoteEndPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught: " + ex.ToString());
                udpClient.Close();
                oscSender.Close();
            }
        }


        static bool IsValidArtNetPacket(byte[] data, out int universe, out int dmxDataLength)
        {
            universe = -1;
            dmxDataLength = 0;
            if (data.Length < 18) return false;
            string header = Encoding.ASCII.GetString(data, 0, 8);
            if (!header.StartsWith("Art-Net\0")) return false;
            int opCode = data[8] + (data[9] << 8);
            if (opCode != 0x5000) return false;
            int protocolVersion = (data[10] << 8) + data[11];
            if (protocolVersion != 14) return false;
            universe = data[14] + (data[15] << 8);
            dmxDataLength = (data[17] << 8) | data[16]; 
            return true;
        }
    }
}
