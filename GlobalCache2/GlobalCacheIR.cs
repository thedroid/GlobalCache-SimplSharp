using System.Collections.Generic;
using Crestron.SimplSharp.CrestronIO;

namespace GlobalCache
{
    public class GlobalCacheIR : GlobalCacheBase
    {
        public  GlobalCacheIR()
        {
        }

        public void ConnectbyMac(string mac, string filename, int module, int connector)
        {
            IRDATA.GetIRDataFromFile(filename);
            ConnectbyMac(mac, module, connector);
        }

        public void ConnectbyIP(string address, string filename, int module, int connector)
        {
            IRDATA.GetIRDataFromFile(filename);
            Connect(address, module, connector);
        }

        public override void Connect(string address, int module, int connector ) 
        {
            base.Connect(address, module, connector);
            sendCommand("set_IR", MODE.IR.ToString());    
        }

        public void refreshIRFile(string filename)
        {
            IRDATA.RefreshDATAfromFile(filename);
        }

        public void sendIR(string filename,int commandIndex)
        {
            string irdata = IRDATA.GetIRCommand(filename, commandIndex - 1);

            if (!irdata.Equals("")) 
                sendCommand("sendir", "1," + irdata);
        }
    }

    static public class IRDATA
    {
        static private Dictionary<string, List<string>> irData = new Dictionary<string, List<string>>();

        static public void GetIRDataFromFile(string filename, bool refresh)
        {
            List<string> irlist = null;

            string shortFilename = Path.GetFileName(filename);

            if (!irData.ContainsKey(shortFilename) || refresh )
                irlist = IRFormats.ReadIRFile(filename);
            
            if (irlist != null)
            {
                if (irData.ContainsKey(shortFilename))
                    irData[shortFilename]=irlist;
                else
                    irData.Add(shortFilename, irlist);
            }
        }

        static public void GetIRDataFromFile(string filename)
        {
            GetIRDataFromFile(filename, false);
        }

        static public void RefreshDATAfromFile(string filename)
        {
            GetIRDataFromFile(filename, true);
        }

        static public string GetIRCommand(string filename, int index)
        {
            string shortFilename = Path.GetFileName(filename);
            string irdata = "";

            if (irData.ContainsKey(shortFilename) && index <= irData[shortFilename].Count)
                irdata = irData[shortFilename][index];

            return irdata;
        }
    }
}