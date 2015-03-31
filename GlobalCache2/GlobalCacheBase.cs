using System.Collections.Generic;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.CrestronSockets;

namespace GlobalCache
{
    public class GlobalCacheBase
    {
        public delegate void ConnectionStateChangeHandler(ushort state);
        public ConnectionStateChangeHandler ConnectionStateChanged { get; set; }

        protected enum MODE
        {
            IR,
            SENSOR,
            SENSOR_NOTIFY,
            IR_BLASTER,
            LED_LIGHTING,
            SERIAL
        };

        private const int GCPORT = 4998;
        private const int GCBUFFERSIZE = 256;
        protected const string BROADCASTADDRESS = "239.255.250.250";
        private const int BEACONPORT = 9131;
        private const int BEACONBUFFERSIZE = 256;

        private string macaddress = "";
        private string address = "";
        protected int module = 1;       
        protected int connector = 1;

        static private SimpleSharpUDPClient BeaconUDPClient;

        private static Dictionary<string, TCPConnectionInfo> connections = new Dictionary<string, TCPConnectionInfo>();

        private class TCPConnectionInfo
        {
            public string macaddress { get; set; }
            public SocketStatus SocketStatus { get; set; }
            public int connectioncount { get; set; }

            public GlobalCacheTCPClient tcpclient { get; set; }

            public TCPConnectionInfo()
            {
                tcpclient = new GlobalCacheTCPClient();
                connectioncount = 1;
            }
        }

        protected GlobalCacheTCPClient gcTCPClientRef;

        private bool enabled = false;

        public GlobalCacheBase()
        {
        }

        public virtual void ConnectbyMac(string mac, int module, int connector)
        {
            this.connector = connector;
            this.module = module;
            this.macaddress = mac;

            // Don't bother listening for the beacon if we already know the ip of the MAC address
            foreach (KeyValuePair <string, TCPConnectionInfo> connection in connections)
            {
                if (connection.Value.macaddress.Equals(mac))
                {
                    Connect(connection.Key, module, connector);
                    return;
                }
            }
            EnableBeacon();
        }

        public virtual void Connect(string address, int module, int connector)
        {
            this.module = module;
            this.connector = connector;
            this.address = address;

            if (!connections.ContainsKey(address))
            {
                connections.Add(address, new TCPConnectionInfo());
                connections[address].macaddress = this.macaddress;

                connections[address].tcpclient.TCPStatusChange += ConnectionStatusChange;
                connections[address].tcpclient.InitializeClient(address, GCPORT, GCBUFFERSIZE);
            }
            else
            {
                connections[address].connectioncount++; 
                onConnectionStatusChange(connections[address].SocketStatus);
                connections[address].tcpclient.TCPStatusChange += ConnectionStatusChange;
            }
            
            enabled = true;
            
            gcTCPClientRef = connections[address].tcpclient; 
        }

        public void ConnectionStatusChange(SocketStatus status)
        {
            connections[address].SocketStatus = status;
            onConnectionStatusChange(status);
        }

        private void onConnectionStatusChange(SocketStatus status)
        {
            if (ConnectionStateChanged != null)
            {
                ushort connectionstate = (status == SocketStatus.SOCKET_STATUS_CONNECTED) ? (ushort)1 : (ushort)0;
                ConnectionStateChanged(connectionstate);
            }
        }

        public virtual void Disconnect()
        {
            enabled = false;
            connections[this.address].connectioncount--;
            connections[address].tcpclient.TCPStatusChange -= ConnectionStatusChange;

            if (connections[this.address].connectioncount == 0)
            {
                connections[address].tcpclient.DisconnectFromServer();
                connections.Remove(address);
            }
            gcTCPClientRef = null;
        }


        public void sendCommand(string command)
        {
                sendCommand(command , "");
        }

        public void sendCommand(string command, string payload)
        {
            if (enabled)
            {
                if (!payload.Equals(""))
                    payload = "," + payload;

                connections[this.address].tcpclient.SendDataToServer(command + "," + module + ":" + connector + payload + "\r");
            }
        }

        public void EnableBeacon()
        {
            if (BeaconUDPClient == null)
                BeaconUDPClient = new SimpleSharpUDPClient();

            BeaconUDPClient.UDPDataAvailable += new SimpleSharpUDPClient.UDPDataAvailableHandler(BeaconHandler);
            BeaconUDPClient.Connect(BROADCASTADDRESS, BEACONPORT, BEACONBUFFERSIZE);
        }

        public void BeaconHandler (string data)
        {

            /*  
             *  AMXB<-UUID=GlobalCache_000C1E0234345><-SDKClass=Utility><-Make=GlobalCache><-Model=iTachWF2IR><-Revision=710-1001-05>
             *  <-Pkg_Level=GCPK001><-Config-URL=http://192.168.111.100.><-PCB_PN=125-2226-06><-Status=Ready>
             */

            
            string regex = @"^AMXB<-UUID=(.*?_(.*?))><-SDKClass=(.*?)><-Make=(.*?)><-Model=(.*?)><-Revision=(.*?)><-Pkg_Level=(.*?)><-Config-URL=(http://(.*?)\.?)><-PCB_PN=(.*?)><-Status=(.*?)>";
            // 1 = UUID , 2 = MAC Address , 3 = SDKClass , 4 = Make , 5 = Model , 6 = Revision , 7 = Package Level , 8 = Config Url , 9 = Device IPAddress , 10 = PCB Part Number , 11 = Status
             
            Regex beaconRegEx = new Regex(regex);
            MatchCollection matches;

            matches = beaconRegEx.Matches(data);

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        var mac = match.Groups[2];
                        if (mac.Value.Equals(this.macaddress))
                        {
                            var ipaddress = match.Groups[9].Value;
                            Connect(ipaddress, this.module, this.connector);
                            DisableBeacon();
                        }       
                    }
                }
            }
        }

        public void DisableBeacon()
        {
            var i = BeaconUDPClient.UDPDataAvailable.GetInvocationList().Length;            

            BeaconUDPClient.UDPDataAvailable -= new SimpleSharpUDPClient.UDPDataAvailableHandler(BeaconHandler);
            if ( i == 1 )
                BeaconUDPClient.Disconnect();
        }
    }
}