using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ChattingServiceServer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private object lockObj = new object();
        private ObservableCollection<string> chattingLogList = new ObservableCollection<string>();
        private ObservableCollection<string> userList = new ObservableCollection<string>();
        private ObservableCollection<string> AccessLogList = new ObservableCollection<string>();
        Task conntectCheckThread = null;

        public MainWindow()
        {
            InitializeComponent();
            string debugCheck = "디버깅용 테스트서버를 사용하시려면 예, 실제 채팅프로그램 사용하려면 아니오를 눌러주세요";
            MessageBoxResult nameMessageBoxResult = MessageBox.Show(debugCheck, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (nameMessageBoxResult == MessageBoxResult.Yes)
            {
                ClientData.isdebug = true;
            }
            else
            {
                ClientData.isdebug = false;
            }
            MainServerStart();
            ClientManager.messageParsingAction += MessageParsing;
            ClientManager.ChangeListViewAction += ChangeListView;
            ChattingLogListView.ItemsSource = chattingLogList;
            UserListView.ItemsSource = userList;
            AccessLogListView.ItemsSource = AccessLogList;
            conntectCheckThread = new Task(ConnectCheckLoop);
            conntectCheckThread.Start();
        }

        private void ConnectCheckLoop()
        {
            while (true)
            {
                foreach (var item in ClientManager.clientDic)
                {
                    try
                    {
                        string sendStringData = "관리자<TEST>";
                        byte[] sendByteData = new byte[sendStringData.Length];
                        sendByteData = Encoding.Default.GetBytes(sendStringData);

                        item.Value.tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);
                    }
                    catch (Exception e)
                    {
                        RemoveClient(item.Value);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private void RemoveClient(ClientData targetClient)
        {
            ClientData result = null;
            ClientManager.clientDic.TryRemove(targetClient.clientNumber, out result);
            string leaveLog = string.Format("[{0}] {1} Leave Server", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), result.clientName);
            ChangeListView(leaveLog, StaticDefine.ADD_ACCESS_LIST);
            ChangeListView(result.clientName, StaticDefine.REMOVE_USER_LIST);
        }

        

        private void ChangeListView(string Message, int key)
        {
            switch (key)
            {
                case StaticDefine.ADD_ACCESS_LIST:
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            AccessLogList.Add(Message);
                        }));
                        break;
                    }
                case StaticDefine.ADD_CHATTING_LIST:
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            chattingLogList.Add(Message);
                        }));
                        break;
                    }
                case StaticDefine.ADD_USER_LIST:
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            userList.Add(Message);
                        }));
                        break;
                    }
                case StaticDefine.REMOVE_USER_LIST:
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            userList.Remove(Message);
                        }));
                        break;
                    }
                default:
                    break;
            }
        }

        private void MessageParsing(string sender, string message)
        {
            lock(lockObj)
            {
                List<string> msgList = new List<string>();

                string[] msgArray = message.Split('>');
                foreach (var item in msgArray)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    msgList.Add(item);
                }
                SendMsgToClient(msgList, sender);
            }
        }

        private void SendMsgToClient(List<string> msgList, string sender)
        {
            string parsedMessage = "";
            string receiver = "";

            int senderNumber = -1;
            int receiverNumber = -1;

            foreach (var item in msgList)
            {
                string[] splitedMsg = item.Split('<');

                receiver = splitedMsg[0];
                parsedMessage = string.Format("{0}<{1}>",sender, splitedMsg[1]);

                if (parsedMessage.Contains("<GroupChattingStart>")) 
                {
                    string[] groupSplit = receiver.Split('#');

                    foreach (var el in groupSplit)
                    {
                        if (string.IsNullOrEmpty(el))
                            continue;
                        string groupLogMessage = string.Format(@"[{0}] [{1}] -> [{2}] , {3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), groupSplit[0], el, splitedMsg[1]);
                        ChangeListView(groupLogMessage, StaticDefine.ADD_CHATTING_LIST);

                        receiverNumber = GetClinetNumber(el);

                        parsedMessage = string.Format("{0}<GroupChattingStart>", receiver);
                        byte[] sendGroupByteData = Encoding.Default.GetBytes(parsedMessage);
                        ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(sendGroupByteData, 0, sendGroupByteData.Length);
                    }
                    return;
                }

                if (receiver.Contains('#'))
                {
                    string[] groupSplit = receiver.Split('#');

                    foreach (var el in groupSplit)
                    {
                        if (string.IsNullOrEmpty(el))
                            continue;
                        if (el == groupSplit[0])
                            continue;
                        string groupLogMessage = string.Format(@"[{0}] [{1}] -> [{2}] , {3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), groupSplit[0], el, splitedMsg[1]);
                        ChangeListView(groupLogMessage, StaticDefine.ADD_CHATTING_LIST);

                        receiverNumber = GetClinetNumber(el);

                        parsedMessage = string.Format("{0}<{1}>", receiver, splitedMsg[1]);
                        byte[] sendGroupByteData = Encoding.Default.GetBytes(parsedMessage);
                        ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(sendGroupByteData, 0, sendGroupByteData.Length);
                    }
                    return;
                }


                senderNumber = GetClinetNumber(sender);
                receiverNumber = GetClinetNumber(receiver);


                if (senderNumber == -1 || receiverNumber == -1)
                {
                    //File.AppendAllText("ClientNumberErrorLog.txt", sender + receiver);
                    return;
                }

                byte[] sendByteData = new byte[parsedMessage.Length];
                sendByteData = Encoding.Default.GetBytes(parsedMessage);

                if (parsedMessage.Contains("<GiveMeUserList>"))
                {
                    string userListStringData = "관리자<";
                    foreach (var el in userList)
	                {
		                userListStringData += string.Format("${0}",el);
	                }
                    userListStringData += ">";
                    byte[] userListByteData = new byte[userListStringData.Length];
                    userListByteData = Encoding.Default.GetBytes(userListStringData);
                    ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(userListByteData,0,userListByteData.Length);
                    return;
                }

                


                string logMessage = string.Format(@"[{0}] [{1}] -> [{2}] , {3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), sender, receiver, splitedMsg[1]);
                ChangeListView(logMessage, StaticDefine.ADD_CHATTING_LIST);

                if (parsedMessage.Contains("<ChattingStart>"))
                {
                    parsedMessage = string.Format("{0}<ChattingStart>", receiver);
                    sendByteData = Encoding.Default.GetBytes(parsedMessage);
                    ClientManager.clientDic[senderNumber].tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);

                    parsedMessage = string.Format("{0}<ChattingStart>", sender);
                    sendByteData = Encoding.Default.GetBytes(parsedMessage);
                    ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);

                    return;
                }

                

                if(parsedMessage.Contains(""))

                ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);
            }
        }

        private int GetClinetNumber(string targetClientName)
        {
            foreach (var item in ClientManager.clientDic)
            {
                if (item.Value.clientName == targetClientName)
                {
                    return item.Value.clientNumber;
                }
            }
            return -1;
        }

        private void MainServerStart()
        {
            MainServer a = new MainServer();
        }
    }
}
