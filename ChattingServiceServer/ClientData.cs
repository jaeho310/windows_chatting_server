using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChattingServiceServer
{
    class ClientData
    {
        public static bool isdebug = false;
        public TcpClient tcpClient { get; set; }
        public Byte[] readBuffer { get; set; }
        public StringBuilder currentMsg { get; set; }
        public string clientName { get; set; }
        public int clientNumber { get; set; }

        public ClientData(TcpClient tcpClient)
        {
            currentMsg = new StringBuilder();
            readBuffer = new byte[1024];

            this.tcpClient = tcpClient;

            char[] splitDivision = new char[2];
            splitDivision[0] = '.';
            splitDivision[1] = ':';

            string[] temp = null;
            if (isdebug)
            {
                temp = tcpClient.Client.LocalEndPoint.ToString().Split(splitDivision);
            }
            else
            {
                temp = tcpClient.Client.RemoteEndPoint.ToString().Split(splitDivision);
            }

            this.clientNumber = int.Parse(temp[3]);
        }
    }
}
