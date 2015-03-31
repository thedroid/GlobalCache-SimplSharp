using System;
using System.Linq;
using System.Text;
using System.Threading;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;

namespace GlobalCache
{
    public class SimpleSharpTCPClient
    {
        protected TCPClient Client;

        public TCPDataAvailableHandler TCPDataAvailable { get; set; }
        public delegate void TCPDataAvailableHandler(String rcvd);

        public TCPSocketStatusChangeHandler TCPStatusChange { get; set; }
        public delegate void TCPSocketStatusChangeHandler(SocketStatus status);

        public SimpleSharpTCPClient()
        {
        }

        public void InitializeClient(String addressToConnectTo, int port, int bufferSize)
        {
            if (Client == null)
            {
                Client = new TCPClient(addressToConnectTo, port, bufferSize);
                Client.SocketStatusChange += new TCPClientSocketStatusChangeEventHandler(SocketStatusChange);
                ConnectToServer();
            }
        }

        private void SocketStatusChange(TCPClient client, SocketStatus status)
        {
            OnStatusChange(status);
        }

        private void OnStatusChange(SocketStatus status)
        {
            if (TCPStatusChange != null)
                TCPStatusChange(status);
        }

        public virtual void ConnectToServer()
        {
            Client.ConnectToServer();
        }

        public virtual void DisconnectFromServer()
        {
            Client.DisconnectFromServer();
        }

        public void SendDataToServer(String data)
        {
            if (Client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                Byte[] dataToSend = Encoding.ASCII.GetBytes(data);

                Client.SendData(dataToSend, dataToSend.Length);
                
                int BytesReceived = Client.ReceiveData();
                String dataReturned = new string(Client.IncomingDataBuffer.Take(BytesReceived).Select(b => (char)b).ToArray());
                onDataAvailable(dataReturned);

            }
        }

        private void onDataAvailable(String data)
        {
            if (TCPDataAvailable != null)
                TCPDataAvailable(data);
        }
    }
}