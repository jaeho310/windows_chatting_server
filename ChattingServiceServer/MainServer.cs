using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChattingServiceServer
{
    public class MainServer
    {
        ClientManager _clientManager = new ClientManager();

        public MainServer()
        {
            Task serverStart = Task.Run(() =>
            {
                ServerRun();
            });
        }
        private void ServerRun()
        {
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, 9999));
            listener.Start();

            while (true)
            {
                Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();

                acceptTask.Wait();

                TcpClient newClient = acceptTask.Result;

                _clientManager.AddClient(newClient);
            }
        }
    }
}
