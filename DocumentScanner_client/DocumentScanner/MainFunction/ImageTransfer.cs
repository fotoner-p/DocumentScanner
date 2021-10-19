using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DocumentScanner
{
    public class ImageTransfer
    {
        private string serverIP;
        private int port;
        private string imagePath;
        private byte[] senderBuff;
        private byte[] receiverBuff = new byte[1024];

        public ImageTransfer(string ip, int pt, string imgPath)
        {
            serverIP = ip;
            port = pt;
            imagePath = imgPath;
        }

        private Socket socketInit()
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var ep = new IPEndPoint(IPAddress.Parse(serverIP), port);
            sock.Connect(ep);

            return sock;
        }

        private void fileSend(Socket sock)
        {
            FileStream filestr = new FileStream(imagePath, FileMode.Open, FileAccess.Read);

            int fileLength = (int)filestr.Length;
            senderBuff = BitConverter.GetBytes(fileLength);

            sock.Send(senderBuff, SocketFlags.None);

            int count = fileLength / 1024 + 1;

            BinaryReader reader = new BinaryReader(filestr);

            for (int i = 0; i < count; ++i)
            {
                senderBuff = reader.ReadBytes(1024);
                sock.Send(senderBuff, SocketFlags.None);
            }

            reader.Close();
        }

        public ValueTuple<int, int>[] LoadPos()
        {
            ValueTuple<int, int>[] positions;
            Socket sock = socketInit();

            senderBuff = Encoding.UTF8.GetBytes("1");
            sock.Send(senderBuff, SocketFlags.None);

            int n = sock.Receive(receiverBuff);

            if (Encoding.UTF8.GetString(receiverBuff, 0, n) == "OK")
            {
                fileSend(sock);

                byte[] flagBuff = new byte[1024];
                n = sock.Receive(flagBuff);
                string flag = Encoding.UTF8.GetString(flagBuff, 0, n);

                if (flag == "1") // pass
                {
                    positions = new ValueTuple<int, int>[4];
                    byte[] buff = new byte[4];

                    for (int i = 0; i < 4; i++)
                    {
                        sock.Receive(buff);
                        int x = BitConverter.ToInt32(buff, 0);

                        sock.Receive(buff);
                        int y = BitConverter.ToInt32(buff, 0);

                        positions[i] = new ValueTuple<int, int>(x, y);
                    }

                    sock.Close();
                    return positions;
                }
            }

            //fail
            positions = new ValueTuple<int, int>[0];

            sock.Close();
            return positions;
        }

        public string[] LoadOCR()
        {
            string[] arr;
            Socket sock = socketInit();

            senderBuff = Encoding.UTF8.GetBytes("2");
            sock.Send(senderBuff, SocketFlags.None);

            int n = sock.Receive(receiverBuff);

            if (Encoding.UTF8.GetString(receiverBuff, 0, n) == "OK")
            {
                fileSend(sock);

                byte[] smallBuffer = new byte[4];

                sock.Receive(smallBuffer);
                int str_length = BitConverter.ToInt32(smallBuffer, 0);
                arr = new string[str_length];

                for (int i = 0; i < str_length; ++i)
                {
                    n = sock.Receive(receiverBuff);

                    arr[i] = Encoding.UTF8.GetString(receiverBuff, 0, n);
                }

                sock.Close();
                return arr;
            }
            //fail
            arr = new string[0];

            sock.Close();
            return arr;
        }
    }
}