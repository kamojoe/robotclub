using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roomba
{
    public class Roomba : IDisposable
    {
        private SerialPort IO;

        public bool IsPortValid { get { return IO.IsOpen; } }

        public bool TryToConnect()
        {
            var ports = SerialPort.GetPortNames();

            foreach (var port in ports)
            {
                var isPortSet = SetPort(port);

                if (isPortSet)
                {
                    return true;
                }
            }

            return false;
        }
        
        private bool SetPort(string portNum) 
        {
            try
            {

                if (IO != null)
                {
                    IO.Close();//Just in case port is already taken
                }

                IO = new SerialPort(portNum, 57600, Parity.None, 8, StopBits.One);
                IO.DtrEnable = false;
                IO.Handshake = Handshake.None;
                IO.RtsEnable = false;

                IO.Open();

                return SendCommand(new List<byte> { 128 });
            }
            catch
            {
                portNum = String.Empty;
                IO.Close();

                return false;
            }
        }

        public bool SendCommand(IEnumerable<byte> commandCollection)
        {
            try
            {
                var commandArr = commandCollection.ToArray();
                IO.Write(commandArr, 0, commandArr.Length);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public byte ReadResponse()
        {
            return (byte)IO.ReadByte();
        }

        public void Dispose()
        {
            if (IO != null)
            {
                IO.Close();
                IO.Dispose();
            }
        }
    }
}
