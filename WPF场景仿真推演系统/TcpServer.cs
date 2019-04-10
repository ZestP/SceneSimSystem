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
        private int myPort = 500;   //端口  
        public Socket serverSocket;
        public Socket clientSocket;

        Thread myThread;
        public Thread receiveThread;

        //属性
        private UnitManager mUnitMan;
        public int port { get; set; }
        //方法


        public TcpServer()
        {
            msgs = new Queue<List<string>>();
        }

        internal void StartServer()
        {
            //服务器IP地址  
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ip, myPort));  //绑定IP地址：端口  
            serverSocket.Listen(10);    //设定最多10个排队连接请求  

            Console.WriteLine("启动监听{0}成功", serverSocket.LocalEndPoint.ToString());

            //通过Clientsoket发送数据  
            myThread = new Thread(ListenClientConnect);
            myThread.Start();

        }
        public void BindRefs(UnitManager um)
        {
            mUnitMan = um;
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
            if (clientSocket != null)
                clientSocket.Send(Encoding.UTF8.GetBytes(msg));
        }



        /// <summary>  
        /// 监听客户端连接  
        /// </summary>  
        private void ListenClientConnect()
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
        private void ReceiveMessage(object clientSocket)
        {
            Socket myClientSocket = (Socket)clientSocket;
            while (true)
            {
                try
                {
                    //通过clientSocket接收数据  
                    int receiveNumber = myClientSocket.Receive(result);
                    string msg = Encoding.UTF8.GetString(result, 0, receiveNumber);
                    Console.WriteLine("接收客户端{0}消息{1}", myClientSocket.RemoteEndPoint.ToString(), msg);
                    Parse(msg);
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

        static Queue<List<string>> msgs;
        void Parse(string raw)
        {
            List<string> tmp = new List<string>(raw.Split(' '));
            if(tmp.Count>0&&tmp[0]=="Spawn"||tmp[0]=="Select"||tmp[0]=="Modify" || tmp[0] == "Add"||tmp[0]=="Disselect")
            {
                msgs.Enqueue(tmp);
            }
        }
        public List<string> GetMsg(string dedicated)
        {
            //s = "";
            //foreach(List<string> ss in msgs)
            //{
            //    foreach(string s2 in ss)
            //    {
            //        s += s2 + ' ';
            //    }
            //    s += '\n';
            //}
            List<string> result = null;
            while (msgs.Count > 0 && msgs.Peek()[0] == dedicated)
            {
                result = msgs.Dequeue();
            }
            return result;
        }
    }

}



//public class StateObject
//    {
//        public Socket workSocket = null;
//        public const int BufferSize = 1024;
//        public byte[] buffer = new byte[BufferSize];
//        public StringBuilder sb = new StringBuilder();
//        public StateObject()
//        {

//        }
//    }
//    class TcpServer
//    {
//        private static IPAddress myIP = IPAddress.Parse("127.0.0.1");
//        private static IPEndPoint MyServer;
//        private static Socket mySocket;
//        private static Socket handler;
//        private static ManualResetEvent myReset = new ManualResetEvent(false);
//        public static void StartServer()
//        {
//            try
//            {
//                MyServer = new IPEndPoint(myIP, 12345);
//                mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//                mySocket.Bind(MyServer);
//                mySocket.Listen(50);
//                Console.WriteLine("Start listening..");
//                Thread thread = new Thread(new ThreadStart(target));
//                thread.Start();
//            }
//            catch
//            {
//                Console.WriteLine("Invalid IP");
//            }

//        }
//        private static void target()
//        {
//            while (true)
//            {
//                try
//                {
//                    myReset.Reset();
//                    mySocket.BeginAccept(new AsyncCallback(AcceptCallback), mySocket);
//                    myReset.WaitOne();
//                }
//                catch
//                {

//                }
//            }
//        }
//        private static void AcceptCallback(IAsyncResult ar)
//        {
//            myReset.Set();
//            Socket listener = (Socket)ar.AsyncState;
//            handler = listener.EndAccept(ar);
//            StateObject state = new StateObject();
//            state.workSocket = handler;
//            try
//            {
//                byte[] byteData = System.Text.Encoding.BigEndianUnicode.GetBytes("Server ready!\n\r");
//                handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
//            } catch (Exception ee) { Console.WriteLine(ee.Message); }
//            Thread thread = new Thread(new ThreadStart(begReceive));
//            thread.Start();
//        }
//        private static void SendCallback(IAsyncResult ar)
//        {
//            try
//            {
//                handler = (Socket)ar.AsyncState;
//                int byteSent = handler.EndSend(ar);
//            }catch(Exception eee)
//            {
//                Console.WriteLine(eee.Message);
//            }
//        }
//        private static void begReceive()
//        {
//            StateObject state = new StateObject();
//            state.workSocket = handler;
//            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
//        }
//        private static void ReadCallback(IAsyncResult ar)
//        {
//            StateObject state = (StateObject)ar.AsyncState;
//            Socket tt = state.workSocket;
//            int bytesRead = handler.EndReceive(ar);
//            state.sb.Append(System.Text.Encoding.BigEndianUnicode.GetString(state.buffer, 0, bytesRead));
//            string content = state.sb.ToString();
//            state.sb.Remove(0, content.Length);
//            Console.WriteLine(content);
//            Parse(content);
//            tt.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
//        }
//        public static void SendMsg(string msg)
//        {
//            try
//            {
//                byte[] byteData = System.Text.Encoding.BigEndianUnicode.GetBytes(msg);
//                handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);

//            }catch(Exception ee) { Console.WriteLine(ee.Message); }
//        }
//        public static void StopServer()
//        {
//            try
//            {
//                mySocket.Close();
//                Console.WriteLine("Server stopped.");
//            }
//            catch
//            {
//                Console.WriteLine("Server failed stopping.");
//            }
//        }

//        public static List<string> GetMsg(string dedicated)
//        {
//            //s = "";
//            //foreach(List<string> ss in msgs)
//            //{
//            //    foreach(string s2 in ss)
//            //    {
//            //        s += s2 + ' ';
//            //    }
//            //    s += '\n';
//            //}
//            List<string> result = null;
//            while (msgs.Count > 0 && msgs.Peek()[0] == dedicated)
//            {
//                result = msgs.Dequeue();
//            }
//            return result;
//        }
//        static Queue<List<string>> msgs;
//        static void Parse(string raw)
//        {
//            if (raw == "Spawn DD")//Test
//                msgs.Enqueue(new List<string>(raw.Split(' ')));

//        }
//    }
//}
