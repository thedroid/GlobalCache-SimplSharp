using System;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;

namespace GlobalCache
{
    public class SimpleSharpUDPClient
    {
        UDPServer UDPClient;
        private bool Enabled = false;

        public UDPDataAvailableHandler UDPDataAvailable { get; set; }
        public delegate void UDPDataAvailableHandler(String datarcvd);

        public SimpleSharpUDPClient()
        {
        }

        public void Connect(String ipaddress, int port, int buffer)
        {
            if (!Enabled)
            {
                if (UDPClient == null)
                    UDPClient = new UDPServer(ipaddress, port, buffer);

                Enable();
            }
        }

        private void Enable()
        {
            Crestron.SimplSharp.CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(CrestronEnvironment_EthernetEventHandler);
            UDPClient.EnableUDPServer();
            Enabled = true;
            UDPClient.ReceiveDataAsync(UdpClientReceiveHandler);
        }

        private void Disable()
        {
            Crestron.SimplSharp.CrestronEnvironment.EthernetEventHandler -= new EthernetEventHandler(CrestronEnvironment_EthernetEventHandler);
            UDPClient.DisableUDPServer();
            Enabled = false;
        }       

        void CrestronEnvironment_EthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            if (ethernetEventArgs.EthernetEventType == eEthernetEventType.LinkUp)
            {
                Enable();
            }
            else
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (Enabled)
            {
                Disable();
            }
        }

        private void UdpClientReceiveHandler(UDPServer UdpClient, int BytesReceived)
        {
            String dataReturned = new string(UdpClient.IncomingDataBuffer.Take(BytesReceived).Select(b => (char)b).ToArray());

            onDataAvailable(dataReturned);
            UdpClient.ReceiveDataAsync(UdpClientReceiveHandler);
        }

        private void onDataAvailable(String data)
        {
            if (UDPDataAvailable != null)
                UDPDataAvailable(data);
        }
    }
}