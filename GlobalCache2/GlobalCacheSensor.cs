using System.Collections.Generic;

namespace GlobalCache
{
    public class GlobalCacheSensor : GlobalCacheBase
    {
        public delegate void SensorStateChangeHandler(ushort state);
        public SensorStateChangeHandler SensorStateChanged { get; set; }

        private int statePORT = 0;

        private static Dictionary<string, UDPConnectionInfo> udpconnections = new Dictionary<string, UDPConnectionInfo>();

        private class UDPConnectionInfo
        {
            public int connectioncount { get; set; }
            public SimpleSharpUDPClient udpclient { get; set; }

            public UDPConnectionInfo()
            {
                udpclient = new SimpleSharpUDPClient();
                connectioncount = 1;
            }
        }

        public  GlobalCacheSensor()
        {
        }

        public void ConnectbyMac(string mac, int module, int connector, int port)
        {
            this.statePORT = port;
            base.ConnectbyMac(mac, module, connector);
        }

        public void ConnectbyIP(string address, int module, int connector, int port)
        {
            this.statePORT = port;
            Connect(address, module, connector);
        }

        public override void Connect(string address, int module, int connector ) 
        {
            base.Connect(address,module,connector);

            gcTCPClientRef.TCPDataAvailable += new GlobalCacheTCPClient.TCPDataAvailableHandler(tcpDataAvailable);

            sendCommand("set_IR", MODE.SENSOR_NOTIFY.ToString());
            EnableStateUDP();
            getSensorState();
        }

        public override void Disconnect()
        {
            gcTCPClientRef.TCPDataAvailable -= new GlobalCacheTCPClient.TCPDataAvailableHandler(tcpDataAvailable);
            DisableStateUDP();
            base.Disconnect();
        }

        public void getSensorState()
        { 
            sendCommand("getstate");
        }

        private void onSensorStateChange(ushort state)
        {
            if (SensorStateChanged != null) SensorStateChanged(state);
        }

        private void EnableStateUDP()
        {
            string key = BROADCASTADDRESS + "-" + this.statePORT;

            if (!udpconnections.ContainsKey( key ))
            {
                udpconnections.Add( key , new UDPConnectionInfo() );
                udpconnections[key].udpclient.UDPDataAvailable += new SimpleSharpUDPClient.UDPDataAvailableHandler(sensorStateHandler);
                udpconnections[key].udpclient.Connect(BROADCASTADDRESS, this.statePORT, 100);
            }
            else
            {
                udpconnections[key].udpclient.UDPDataAvailable += new SimpleSharpUDPClient.UDPDataAvailableHandler(sensorStateHandler);
                udpconnections[key].connectioncount++;
            }
        }

        private void DisableStateUDP()
        {
            string key = BROADCASTADDRESS + "-" + this.statePORT; 
            
            udpconnections[key].udpclient.UDPDataAvailable -= new SimpleSharpUDPClient.UDPDataAvailableHandler(sensorStateHandler);
            udpconnections[key].connectioncount--;

            if (udpconnections[key].connectioncount == 0)
            {
                udpconnections[key].udpclient.Disconnect();
                udpconnections.Remove(key);
            }
        }

        private void tcpDataAvailable(string data)
        {
            string address = string.Empty + module + ":" + connector;
           
            if (data.Equals("state," + address + ",0\r"))
                onSensorStateChange(0);
            else if (data.Equals("state," + address + ",1\r"))
                onSensorStateChange(1);
        }

        private void sensorStateHandler(string data)
        {
            string address = string.Empty + module + ":" + connector;

            if (data.Equals("sensornotify," + address + ",0\r"))
                onSensorStateChange(0);
            else if (data.Equals("sensornotify," + address + ",1\r"))
                onSensorStateChange(1);
        }
    }
}