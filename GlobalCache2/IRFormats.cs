using System;
using System.Collections.Generic;
using Crestron.SimplSharp.CrestronIO;

namespace GlobalCache
{
    static public class IRFormats
    {
        const byte FIELD_FILE_TYPE = 0xF0;
        const byte FIELD_HEADER_END = 0xFF;

        static public List<string> ReadIRFile(string filename)
        {
            List<string> irlist = null;

            if (File.Exists(filename))
            {
                if (Path.GetExtension(filename).ToLower().Equals(".ir"))
                    irlist = ReadCrestronIrFile(filename);
                else if (Path.GetExtension(filename).ToLower().Equals(".ccf"))
                    irlist = ReadCCFTextIRFile(filename);
            }
            return irlist;
        }

        static private List<string> ReadCrestronIrFile(String filename)
        {
            List<string> irlist = new List<string>();

            if (!File.Exists(filename)) return null;

            using (BinaryReader b = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                int pos = 1;

                int length = (int)b.BaseStream.Length;
                while (pos < length)
                {
                    int FieldLength = b.ReadByte();
                    byte fieldType = b.ReadByte();

                    if (fieldType >= FIELD_FILE_TYPE && fieldType <= FIELD_HEADER_END)
                    {
                        var bytearray = b.ReadBytes(FieldLength - 2);
                        String TmpSt = System.Text.Encoding.ASCII.GetString(bytearray, 0, bytearray.Length);
                        pos += FieldLength;

                        switch (fieldType)
                        {
                            case FIELD_FILE_TYPE:
                                if (!TmpSt.Equals("IR")) return null;
                                break;

                            case FIELD_HEADER_END:
                                // Read IR Command data
                                while (pos < length)
                                {
                                    FieldLength = b.ReadByte();
                                    b.ReadByte();
                                    var irarray = crestronToGC(b.ReadBytes(FieldLength - 2));
                                    irlist.Add(irarray);
                                    pos += FieldLength;
                                }
                                break;
                        }
                    }
                    else if (fieldType < 0xF0)
                    {
                        var bytearray = b.ReadBytes(FieldLength - 2);
                        pos += FieldLength;
                    }
                }
            }
            return irlist;
        }
    
        static private List<string> ReadCCFTextIRFile(String filename)
        {
            List<string> irlist = new List<string>();

            if (!File.Exists(filename)) return null;

            string line = "";

            using (Crestron.SimplSharp.CrestronIO.StreamReader file = new Crestron.SimplSharp.CrestronIO.StreamReader(filename))
            {
                while ((line = file.ReadLine()) != null)
                {
                    var irarray = CCFtoGC(line);
                    if (irarray != null) irlist.Add(irarray);
                }
            }
            return irlist;
        }


        static public string crestronToGC(byte[] crestronIR)
        {
            int data_CCFFreq = 4000000 / crestronIR[0];
            int[] indexedInteger = new int[15];
            int data_OneTimeIndexed = crestronIR[3] & 0x0F;

            var data_Length = crestronIR.Length - 4;
            var newArray = new byte[data_Length];

            Array.Copy(crestronIR, 4, newArray, 0, newArray.Length);

            string gc = string.Empty + data_CCFFreq + ",1,1";

            for (var y = 0; y < data_OneTimeIndexed; y++)
            {
                indexedInteger[y] = (newArray[y * 2] << 8) + newArray[1 + (y * 2)];
            }

            for (var y = 0; y < data_Length - (data_OneTimeIndexed * 2); y++)
            {
                var IndexHighByte = (newArray[(data_OneTimeIndexed * 2) + y] & 0xF0) >> 4;
                var IndexLowByte = newArray[(data_OneTimeIndexed * 2) + y] & 0x0F;
                gc += "," + indexedInteger[IndexHighByte] + "," + indexedInteger[IndexLowByte];
            }
            return gc;
        }

        static public string CCFtoGC(string strInput)
        {
            strInput = strInput.Trim();

            if (!CCFValid(strInput))
                return null;

            string[] strArray = strInput.Split(' ');

            int length = strArray.Length;

            int num = (((0xa1ea / int.Parse(strArray[1], System.Globalization.NumberStyles.HexNumber)) + 5) / 10) * 0x3e8;

            string gc = num.ToString() + ",1,1";
            for (int i = 4; i < length; i++)
            {
                gc += "," + int.Parse(strArray[i], System.Globalization.NumberStyles.HexNumber).ToString();
            }
            return gc;
        }

        static public bool CCFValid(string strInput)
        {
            int length = strInput.Length;
            char[] chArray = strInput.ToCharArray();
            for (int i = 0; i < length; i++)
            {
                if ((((chArray[i] < '0') || (chArray[i] > '9')) && ((chArray[i] < 'a') || (chArray[i] > 'f'))) && (((chArray[i] < 'A') || (chArray[i] > 'F')) && (chArray[i] != ' ')))
                {
                    return false;
                }
            }
            return ((length >= 0x1d) && (length <= 0x513));
        }
    }
}