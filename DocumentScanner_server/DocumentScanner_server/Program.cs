using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using OpenCvSharp;

namespace DocumentScanner_server
{
    class Program
    {
        static int MAX_SIZE = 1024;  // 가정
        static string logTime;
        static void Main(string[] args)
        {
            RunAsyncSocketServer().Wait();
        }

        static async Task RunAsyncSocketServer()
        {
            // (1) 소켓 객체 생성 (TCP 소켓)
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // (2) 포트에 바인드
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 11000);
            sock.Bind(ep);

            // (3) 포트 Listening 시작
            sock.Listen(100);


            Console.WriteLine("server START---------------------------------\n");
            while (true)
            {
                // (4) 비동기 소켓 Accept
                Socket clientSock = await Task.Factory.FromAsync(sock.BeginAccept, sock.EndAccept, null);

                // (5) 비동기 소켓 수신
                var buff = new byte[MAX_SIZE];

                int nCount = await Task.Factory.FromAsync<int>(
                           clientSock.BeginReceive(buff, 0, buff.Length, SocketFlags.None, null, clientSock),
                           clientSock.EndReceive);

                if (nCount > 0)
                {

                    logTime = DateTime.Now.ToString("yyyy-MM-dd-hh시mm분ss초");
                    string msg = Encoding.UTF8.GetString(buff, 0, nCount);

                    Console.WriteLine("recv from '" + clientSock.RemoteEndPoint.ToString() + "' (" + logTime + ")");
                    Console.WriteLine("Client state " + msg);

                    if (msg == "1")
                    {
                        byte[] smallBuffer = new byte[4];

                        await FileTransferAsync(clientSock);

                        Console.WriteLine("openCV mode-------------");

                        IplImage src = new IplImage(logTime + ".jpg", LoadMode.AnyColor);

                        CvPoint[] resultPos = preprocessImg(src);

                        if (resultPos.Count() == 4)
                        {
                            byte[] strBuff = new byte[1024];
                            strBuff = Encoding.UTF8.GetBytes("1");

                            clientSock.Send(strBuff, SocketFlags.None);

                            foreach (var pos in resultPos)
                            {
                                Console.WriteLine(pos.ToString());
                                smallBuffer = BitConverter.GetBytes(pos.X);
                                clientSock.Send(smallBuffer, SocketFlags.None);

                                smallBuffer = BitConverter.GetBytes(pos.Y);
                                clientSock.Send(smallBuffer, SocketFlags.None);
                            }
                        }

                        else
                        {
                            buff = Encoding.UTF8.GetBytes("-1");

                            await Task.Factory.FromAsync(
                                    clientSock.BeginSend(buff, 0, buff.Length, SocketFlags.None, null, clientSock),
                                    clientSock.EndSend);
                        }
                    }

                    else if(msg == "2")
                    {
                        await FileTransferAsync(clientSock, true);
                        //OCR
                        Console.WriteLine("OCR mode-------------");
                        LinkedList<string> result = GoogleOcr.readingText(logTime + "(OCR).jpg");

                        byte[] result_length = BitConverter.GetBytes(result.Count());
                        clientSock.Send(result_length, SocketFlags.None);

                        foreach (var str in result)
                        {
                            buff = Encoding.UTF8.GetBytes(str);

                            Console.WriteLine(" "+str);

                            await Task.Factory.FromAsync(
                                    clientSock.BeginSend(buff, 0, buff.Length, SocketFlags.None, null, clientSock),
                                    clientSock.EndSend);
                        }
                    }
                }
                Console.WriteLine("Client Socket Close-------------------------------------------\n");
                // (7) 소켓 닫기
                clientSock.Close();
            }
        }

        static public async Task FileTransferAsync(Socket sock, bool ocrFlag = false)
        {
            var buff = new byte[MAX_SIZE];

            buff = Encoding.UTF8.GetBytes("OK");

            await Task.Factory.FromAsync(
                    sock.BeginSend(buff, 0, buff.Length, SocketFlags.None, null, sock),
                    sock.EndSend);

            byte[] smallBuffer = new byte[4];

            sock.Receive(smallBuffer);
            int fileLength = BitConverter.ToInt32(smallBuffer, 0);

            int totalLength = 0;
            FileStream fileStr;

            if (ocrFlag)
                fileStr = new FileStream(logTime + "(OCR).jpg", FileMode.Create, FileAccess.Write);
            else
                fileStr = new FileStream(logTime + ".jpg", FileMode.Create, FileAccess.Write);

            BinaryWriter writer = new BinaryWriter(fileStr);

            Console.WriteLine("Start recv File");

            while (totalLength < fileLength)
            {
                int receiveLength = sock.Receive(buff);

                writer.Write(buff, 0, receiveLength);
                totalLength += receiveLength;
            }

            Console.WriteLine("Complete recv File");
            writer.Close();

            return;
        }

        static public CvPoint[] preprocessImg(IplImage ipl)
        {
            IplImage src;
            IplImage result;
            IplImage gray;
            IplImage edge;
            IplImage borders;

            CvPoint[] points = new CvPoint[0];

            using (var convert = new OpenCV())
            {
                src = ipl.Clone();
                borders = ipl.Clone();

                result = null;

                gray = convert.GrayScale(ipl);
                Cv.Smooth(gray, gray, SmoothType.Gaussian);
                gray = convert.DilateImage(gray).Clone();
                edge = convert.CannyEdge(gray).Clone();

                CvMemStorage Storage = new CvMemStorage();
                CvSeq<CvPoint> contours;
                Cv.FindContours(edge, Storage, out contours, CvContour.SizeOf, ContourRetrieval.List, ContourChain.ApproxNone);

                double max = Double.MinValue;
                

                for (CvSeq<CvPoint> c = contours; c != null; c = c.HNext)
                {
                    CvPoint[] ptseq = new CvPoint[c.Total];
                    double peri = 0;

                    for (int i = 0; i < c.Total; i++)
                    {
                        CvPoint? p = Cv.GetSeqElem(c, i);
                        ptseq[i] = new CvPoint { X = p.Value.X, Y = p.Value.Y };
                    }

                    for (int i = 0; i < c.Total - 1; i++)
                    {
                        peri += ptseq[i].DistanceTo(ptseq[i + 1]);
                    }
                    peri += ptseq[c.Total - 1].DistanceTo(ptseq[0]);

                    if (peri > max)
                    {
                        CvSeq<CvPoint> approx = Cv.ApproxPoly(c, CvContour.SizeOf, Storage, ApproxPolyMethod.DP, 0.05 * peri, true);

                        if (approx.Total == 4)
                        {
                            CvPoint[] hull;

                            Cv.ConvexHull2(approx, out hull, ConvexHullOrientation.Clockwise);

                            CvPoint pt0 = hull.Last();

                            points = hull;
                            max = peri;
                        }
                    }
                }
            }
            Cv.ReleaseImage(gray);
            Cv.ReleaseImage(result);
            Cv.ReleaseImage(edge);
            Cv.ReleaseImage(src);

            if (gray != null) gray.Dispose();
            if (edge != null) edge.Dispose();
            if (result != null) result.Dispose();
            if (src != null) src.Dispose();

            return points;
        }
    }
}
