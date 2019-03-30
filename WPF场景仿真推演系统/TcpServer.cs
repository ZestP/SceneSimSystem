using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WPF场景仿真推演系统
{
    class TcpServer
    {
        //私有成员
        private static byte[] result = new byte[1024];
        private int myProt = 500;   //端口  
        static Socket serverSocket;
        static Socket clientSocket;

        Thread myThread;
        static Thread receiveThread;

        //属性

        public int port { get; set; }
        //方法



        internal void StartServer()
        {
            //服务器IP地址  
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ip, myProt));  //绑定IP地址：端口  
            serverSocket.Listen(10);    //设定最多10个排队连接请求  

            Console.WriteLine("启动监听{0}成功", serverSocket.LocalEndPoint.ToString());

            //通过Clientsoket发送数据  
            myThread = new Thread(ListenClientConnect);
            myThread.Start();

        }


        internal void QuitServer()
        {

            if (serverSocket != null) serverSocket.Close();
            if (clientSocket != null) clientSocket.Close();
            if (myThread != null) myThread.Abort();
            if (receiveThread != null) receiveThread.Abort();


        }


        internal void SendMessage(string msg)
        {
            if(clientSocket!=null)
                clientSocket.Send(Encoding.UTF8.GetBytes(msg));
        }



        /// <summary>  
        /// 监听客户端连接  
        /// </summary>  
        private static void ListenClientConnect()
        {
            while (true)
            {
                try
                {
                    clientSocket = serverSocket.Accept();
                    clientSocket.Send(Encoding.UTF8.GetBytes("Server Say Hello"));
                    receiveThread = new Thread(ReceiveMessage);
                    receiveThread.Start(clientSocket);
                }
                catch (Exception)
                {

                }

            }
        }

        /// <summary>  
        /// 接收消息  
        /// </summary>  
        /// <param name="clientSocket"></param>  
        private static void ReceiveMessage(object clientSocket)
        {
            Socket myClientSocket = (Socket)clientSocket;
            while (true)
            {
                try
                {
                    //通过clientSocket接收数据  
                    int receiveNumber = myClientSocket.Receive(result);
                    Console.WriteLine("接收客户端{0}消息{1}", myClientSocket.RemoteEndPoint.ToString(), Encoding.UTF8.GetString(result, 0, receiveNumber));
                }
                catch (Exception ex)
                {
                    try
                    {
                        Console.WriteLine(ex.Message);
                        myClientSocket.Shutdown(SocketShutdown.Both);
                        myClientSocket.Close();
                        break;
                    }
                    catch (Exception)
                    {
                    }

                }
            }
        }
    }
}
