using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Home.Server.Daemons;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Gpio;
using Devices;
using System.Net.Sockets;
using Home.Server.Hubs;
using System.Net;



namespace Home.Server.Repositories
{

    // The Home hub interface
    // This stores all the Devices 
    public interface IHomeData
    {
        void ReceiveMsg(string sMsg);  //-> interfaces are already public
        string GetDeviceGroup(string sGroupName, bool bAutoregister = true); //Should remove public?-> interfaces are already public
    }
    public static class MSG
    {
        // constants for msg packet breakup
        // Orig:ID^Target:ID^Command^Data^msgLen
        public const int Orig = 0;
        public const int OrigID = 1;
        public const int Target = 2;
        public const int TargetID = 3;
        public const int Command = 4;
        public const int Data = 5;

    }
    public class CmdProc
    {
        protected HomeDC hDC = null;
        public CmdProc()
        {
        }
        public void setDC(HomeDC dC)
        {
            hDC = dC;
        }
        public virtual string Name { get => ""; }
        public virtual void ReceiveMsg(string[] sMsg)
        {

        }
        //Return 
        public virtual CmdProc GetCmdProc()
        {
            // return a new object we could return this if we wanted a singleton implementation
            return new CmdProc();
        }
        public virtual void Do(string[] MsgPack)
        {

        }
    }
    public class Switch : CmdProc
    {
        public override string Name { get => "switch"; }
        public Switch()
        {
        }
        public override void ReceiveMsg(string[] sMsg)
        {

        }
        public override CmdProc GetCmdProc()
        {
            return new Switch();
        }
        // The actual work!!
        // search the device registry , find the target group / ID
        // and forward the message to it
        public override void Do(string[] MsgPack)
        {

        }

    }
    public class Register : CmdProc
    {
        public override string Name { get => "register"; }
        public Register()
        {
        }
        public override void ReceiveMsg(string[] sMsg)
        {

        }
        public override CmdProc GetCmdProc()
        {
            return new Register();
        }
        // The actual work!!
        // search the device registry , find the target group / ID
        // and forward the message to it
        public override void Do(string[] MsgPack)
        {
            // MsgPack[MSG.OrigID] => IDevice , 
            // MsgPack[MSG.Orig] => default group if any
            // MsgPack[MSG.Data] => format => IPAddress:Port:TargetType:TargetID
            // sDatabits[0] // IPAddress
            // sDatabits[1] // Port 
            // sDatabits[2] // TargetType 
            // sDatabits[3] // TargetID

            string[] sDatabits = MsgPack[MSG.Data].Split(':');

            Device d = new Device();
            d.DeviceGroup = MsgPack[MSG.Orig];
            d.ID = MsgPack[MSG.OrigID];
            d.IPEnd = new IPEndPoint(IPAddress.Parse(sDatabits[0]), int.Parse(sDatabits[1]));
            d.TargetType = sDatabits[2];
            d.TargetID = sDatabits[3];
            hDC.Register(d);
        }

    }

    public class Device
    {
        private string _ID;
        private string _DeviceGroup;
        private IPEndPoint _ipEP;
        private ILogger<Device> _logger;
        private string _TargetType; // This acts upon a particular type like a switch or button could target Relays / MCUs
        private string _TargetID;

        public string ID { get => _ID; set => _ID = value; }
        public string TargetType { get => _TargetType; set => _TargetType = value; }
        public string TargetID { get => _TargetID; set => _TargetID = value; }
        public string DeviceGroup { get => _DeviceGroup; set => _DeviceGroup = value; }
        public IPEndPoint IPEnd { get => _ipEP; set => _ipEP = value; }
        public Device() { }
        protected void SetLogger(ILogger<Device> log)
        {
            _logger = log;
        }

        protected virtual void Do() { }

    }

    public class HomeDC : IHomeData
    {

        Dictionary<string, CmdProc> processors = null;
        List<Device> devices;
        Dictionary<string, List<Device>> gpDevices;

        private void FillProcs()
        {
            // Read from a file that provides all the implementation classes in other assemblies
            // to make it extensible without touching this main file

            CmdProc c = new Switch().GetCmdProc();
            c.setDC(this);
            processors.Add(c.Name, c);
        }
        public HomeDC()
        {
            processors = new Dictionary<string, CmdProc>();
            devices = new List<Device>();
            gpDevices = new Dictionary<string, List<Device>>();
        }
        public void ReceiveMsg(string sMsg)
        {
            string[] MsgPack = sMsg.Split(new char[] { '^', ':' });

            if (processors.ContainsKey(MsgPack[MSG.Command]))
                processors[MsgPack[MSG.Command]].Do(MsgPack);  // retrieve the particular command processor 


        }
        // for SignalR clients???
        public void Register(string sMsg, bool isGroup = true, List<string> sDeviceIDs)
        {
            //            if (isGroup)
            //                    gpDevices.TryAdd();
        }

        public void Register(Device d)
        {
            devices.Add(d);
        }
        public Device GetDevice(string ID)
        {
            return devices.Find(devices => devices.ID == ID);
        }
        public bool GetDeviceGroup(string sGroupName, bool bAutoregister = true)
        {
            if (gpDevices.ContainsKey())
                return true;
            else
            {
                if (bAutoregister)
                {
                    gpDevices.Add(sGroupName, new List<Device>());
                    return true;
                }
            }
            return false;
        }

        public List<Device> GetDevices(string sGroup)
        {

        }

        public void Register(string sMsg)
        {
        }
    }
}