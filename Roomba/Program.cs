using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roomba
{
    class Program
    {
        static void Main(string[] args)
        {
            var roomba = new RoombaController();

            var result = roomba.Connect();

            roomba.SwitchToFullMode();

            roomba.Drive(300, 2001);

            roomba.Disconnect();

        }
    }
}




