using System;
using System.Linq;
using System.Text;
using System.Threading;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;

namespace GlobalCache
{
    public class GlobalCacheTCPClient : SimpleSharpTCPClient
    {

        private static object _lock = new object();
        private CTimer timer;
        private const int KEEPALIVETIMER = 60000;

        public GlobalCacheTCPClient()
        {
        }

        public override void ConnectToServer()
        {
            base.ConnectToServer();
            if (timer != null)
                timer.Reset(KEEPALIVETIMER);
            else
                timer = new CTimer(RunCycle, KEEPALIVETIMER);
        }

        public override void DisconnectFromServer()
        {
            if (timer != null)
                timer.Stop();

            base.DisconnectFromServer();
        }

        private void RunCycle(object o) 
        {
            if (!Monitor.TryEnter(_lock))
                return;
            try
            {
                if (base.Client != null)
                    keepAlive();
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }
        
        /// <summary>
        /// Unfortunately the SocketStatusChange Event does not update until we try to write to or read from a socket
        /// which  will be too late for a reconnect set a timer and check the connection at intervals and keep
        /// trying to reconnect to the device if the link is broken.
        /// </summary>
        private void keepAlive()
        {
            try
            {
                base.Client.SendData(new byte[] {0x0C} , 1);
            }
            finally 
            {
                if (base.Client.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED)
                    ConnectToServer();
                else
                    timer.Reset(KEEPALIVETIMER);
            }
        }
    }
}