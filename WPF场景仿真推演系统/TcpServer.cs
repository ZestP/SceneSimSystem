using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WPF场景仿真推演系统
{
    public class TcpServer
    {
        public class NameData
        {
            public string Name { get; set; }
            public NameData(string n)
            {
                Name = n;
            }
        }
        public enum TcpServerMode { EDITOR,PLAYER}
        //私有成员
        private byte[] result = new byte[1024];
        private int myPort = 500;   //端口  
        public Socket serverSocket;
        public List<Socket> clientSockets;
        public List<bool> clientLinked;
        public Dictionary<Socket, int> clientDict;
        public ObservableCollection<NameData> linkedClientNames;
        Thread myThread;
        public Thread receiveThread;

        //属性
        private UnitManager mUnitMan;
        private int mMaxConnection=10;
        //方法
        private TcpServerMode mMode=TcpServerMode.EDITOR;
        public IPAddress mIP= IPAddress.Parse("127.0.0.1");
        public TcpServer(IPAddress ip,int port,int maxCon,TcpServerMode mode)
        {
            mMode = mode;
            mIP = ip;
            myPort = port;
            mMaxConnection = maxCon;
            msgs = new Queue<List<string>>();
            clientSockets = new List<Socket>();
            clientDict = new Dictionary<Socket, int>();
            clientLinked = new List<bool>();
            linkedClientNames = new ObservableCollection<NameData>();
        }

        internal void StartServer()
        {
            //服务器IP地址  
            
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(mIP, myPort));  //绑定IP地址：端口  
            serverSocket.Listen(mMaxConnection);    //设定最多10个排队连接请求  
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
            if (clientSockets != null) foreach(Socket s in clientSockets)s.Close();
            if (myThread != null) myThread.Abort();
            if (receiveThread != null) receiveThread.Abort();


        }


        internal void SendMessage(string msg)
        {
            if (clientSockets != null)
                foreach(Socket s in clientSockets)if(clientLinked[clientDict[s]])s.Send(Encoding.UTF8.GetBytes(msg+(char)(5)));
            
        }
        public ListBox lb;
        public void PrepareDisplayList()
        {
            
            linkedClientNames = new ObservableCollection<NameData>();
            for(int i=0;i<clientLinked.Count;i++)
            {
                if(clientLinked[i])
                {
                    linkedClientNames.Add(new NameData(clientSockets[i].RemoteEndPoint.ToString()));
                    //Console.WriteLine("ADD display" + clientSockets[i].RemoteEndPoint.ToString());
                }
            }
            if(lb!=null)
                lb.DataContext = linkedClientNames;
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
                    Socket ts = serverSocket.Accept();
                    clientSockets.Add(ts);
                    clientDict.Add(ts, clientSockets.Count - 1);
                    clientLinked.Add(true);
                    PrepareDisplayList();
                    ts.Send(Encoding.UTF8.GetBytes("Server Say Hello"));
                    receiveThread = new Thread(ReceiveMessage);
                    receiveThread.Start(ts);
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
                    if (!clientLinked[clientDict[myClientSocket]]) return;
                    //通过clientSocket接收数据  
                    int receiveNumber = myClientSocket.Receive(result);
                    if(receiveNumber==0)
                    {
                        CloseConnection(myClientSocket);
                    }
                    string msg = Encoding.UTF8.GetString(result, 0, receiveNumber);
                    Console.WriteLine("接收客户端{0}消息{1}", myClientSocket.RemoteEndPoint.ToString(), msg);
                    if (mMode == TcpServerMode.EDITOR)
                        Parse(msg);
                    else if (mMode == TcpServerMode.PLAYER)
                        ParseClient(msg);
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
        void CloseConnection(Socket client)
        {
            if (client != null)
            {
                clientLinked[clientDict[client]] = false;
                PrepareDisplayList();
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                
                
            }

        }
        static Queue<List<string>> msgs;
        void Parse(string raw)
        {
            List<string> tmp = new List<string>(raw.Split(' '));
            if(tmp.Count>0&&tmp[0]=="Spawn"||tmp[0]=="Select"||tmp[0]=="Modify" || tmp[0] == "Add"||tmp[0]=="Disselect"|| tmp[0] == "Fire"||tmp[0]=="SyncTime")
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
            if (msgs.Count > 0 && msgs.Peek()[0] == dedicated)
            {
                result = msgs.Dequeue();
            }
            return result;
        }
        void ParseClient(string raw)
        {

        }
        public void SendFile(string filePath, string fileName)
        {

            foreach(Socket socketSend in clientSockets)
            {
                if (clientLinked[clientDict[socketSend]])
                {
                    try
                    {
                        //读取选择的文件
                        using (FileStream fsRead = new FileStream(filePath + "\\" + fileName, FileMode.OpenOrCreate, FileAccess.Read))
                        {
                            //1. 第一步：发送一个包，表示文件的长度，让客户端知道后续要接收几个包来重新组织成一个文件
                            long length = fsRead.Length;
                            byte[] byteLength = Encoding.UTF8.GetBytes(""+(char)(1)+filePath +" "+fileName+" "+length.ToString()+" "+(char)(2));
                            //获得发送的信息时候，在数组前面加上一个字节 1
                            List<byte> list = new List<byte>();
                            list.AddRange(byteLength);
                            socketSend.Send(list.ToArray()); //
                                                             //2. 第二步：每次发送一个4KB的包，如果文件较大，则会拆分为多个包
                            byte[] buffer = new byte[1024 * 1024];
                            long send = 0; //发送的字节数                  
                            while (true)  //大文件断点多次传输
                            {
                                int r = fsRead.Read(buffer, 0, buffer.Length);
                                if (r == 0)
                                {
                                    break;
                                }else if(r<buffer.Length)
                                {
                                    byte[] buffer2 = new byte[r];
                                    for(int i=0;i<r;i++)
                                    {
                                        buffer2[i] = buffer[i];
                                    }
                                    buffer = buffer2;
                                }
                                socketSend.Send(buffer);
                                send += r;
                                Console.WriteLine(string.Format("{0}: 已发送：{1}/{2}", socketSend.RemoteEndPoint, send, length));
                            }

                            socketSend.Send(Encoding.UTF8.GetBytes(""+(char)(3)));
                            Console.WriteLine("发送完成");

                        }
                    }
                    catch
                    {

                    }
                }
            }
            
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
