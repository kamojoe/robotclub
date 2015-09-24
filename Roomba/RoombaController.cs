using System;
using System.Collections.Generic;
using System.Linq;

namespace Roomba
{
    public class RoombaController
    {
        private readonly Roomba _roomba = new Roomba();
        private OperationMode _currentMode = OperationMode.Off;

        public enum OperationMode
        {
            Off, Passive, Safe, Full
        }

        public enum Packet
        {
            BumperOrWheelDrop = 7,
            Wall = 8,
            CliffLeft = 9,
            CliffFrontLeft = 10,
            CliffRight = 11,
            CliffFrontRight = 12,
            VirtualWall = 13,
            WheelOvercurrent = 14,
            Distance = 19,
            Angle = 20,
            ChargingState = 21,
            BatteryCharge = 25,
            OiMode = 35,
            SongNumber = 36,
            SongPlaying = 37
        }

        #region Connections

        /// <summary>
        /// Starts the OI.
        /// Each session, must always use this command before sending any other commands.
        /// </summary>
        /// <remarks>
        /// Available in modes: Passive, Safe, or Full
        /// Changes mode to: Passive. Roomba beeps once to acknowledge it is starting from “off” mode
        /// </remarks>
        public bool Connect()
        {
            _currentMode = OperationMode.Passive;
            return _roomba.TryToConnect();
        }

        /// <summary>
        /// Stops the OI.
        /// Use this when finished sending commands.
        /// </summary>
        /// <remarks>
        /// Available in modes: Passive, Safe, or Full 
        /// Changes mode to: Off. Roomba plays a song to acknowledge it is exiting the OI.
        /// </remarks>
        public bool Disconnect()
        {
            _currentMode = OperationMode.Off;
            return SendCommand(173);
        }

        /// <summary>
        /// Resets the Robot, as if you had removed and reinserted the Battery.
        /// </summary>
        /// <remarks>
        /// Available in modes: Always available.
        /// Changes mode to: Off. You will have to Connect again to re-enter Open Interface mode. 
        /// </remarks>
        public bool Reset()
        {
            _currentMode = OperationMode.Off;
            return SendCommand(7);
        }

        #endregion

        #region Modes

        /// <summary>
        /// This command puts the OI into Safe mode, enabling user control of Roomba. It turns off all LEDs.
        /// </summary>
        /// <remarks>
        /// The OI can be in Passive, Safe, or Full mode to accept this command. 
        /// If a safety condition occurs Roomba reverts automatically to Passive mode. 
        /// </remarks>
        public bool SwitchToSafemode()
        {
            _currentMode = OperationMode.Safe;
            return SendCommand(131);
        }

        /// <summary>
        /// This command gives you complete control over Roomba by putting the OI into Full mode, 
        /// and turning off the cliff, wheel-drop and internal charger safety features.
        /// That is, in Full mode, Roomba executes any command that you send it, 
        /// even if the internal charger is plugged in, or command triggers a cliff or wheel drop condition. 
        /// </summary>
        /// <remarks>
        /// Available in modes: Passive, Safe, or Full 
        /// Changes mode to: Full 
        /// </remarks>
        public bool SwitchToFullMode()
        {
            _currentMode = OperationMode.Full;
            return SendCommand(132);
        }

        /// <summary>
        /// Returns the current Mode that the Roomba is operating in.
        /// </summary>
        public OperationMode GetCurrentMode()
        {
            return _currentMode;
        }

        #endregion

        #region Movement

        /// <summary>
        /// This command controls Roomba’s drive wheels. 
        /// 
        /// Velocity represents average velocity of the drive wheels in millimeters / second.
        /// Radius represents distance between center of turning circle to center of Roomba.
        /// 
        /// Positive velocity is forward, negative is backwards.
        /// Positive radius turns left, negative turns right.
        /// 
        /// There are 3 special cases for Radius:
        /// A Radius of 0 will drive the Roomba straight, no turning.
        /// A Radius of 2001 will turn the Roomba in-place clockwise.
        /// A Radius of 2002 will turn the Roomba in-place counter-clockwise.
        /// </summary>
        /// <param name="velocity">Velocity value between -500 and 500. Represents mm/s.</param>
        /// <param name="radius">Radius value between -2000 and 2000. Represents mm.</param>
        /// <remarks>
        /// Available in modes: Safe or Full
        /// 
        /// Internal and environmental restrictions may prevent Roomba from accurately carrying out some drive commands.
        /// For example, it may not be possible for Roomba to drive at full speed in an arc with a large radius of curvature. 
        /// </remarks>
        /// <example>
        /// Drive slowly forward and turn sharply left: Drive(100, 1600)
        /// Drive quickly backward and turn mildly right: Drive(-500, -300)
        /// Turn in-place counter-clockwise quickly: Drive(400, 2002)
        /// </example>
        public bool Drive(int velocity, int radius)
        {
            velocity.RestrictToRange(-500, 500);
            radius.RestrictToRange(-2000, 2000);

            var velocityBytes = velocity.ToBytes();
            var radiusBytes = radius.ToBytes();

            // Special cases
            switch (radius)
            {
                case 0:
                    radiusBytes = 32768.ToBytes();
                    break;
                case 2001:
                    radiusBytes = (-1).ToBytes();
                    break;
                case 2002:
                    radiusBytes = 1.ToBytes();
                    break;
            }

            if (velocityBytes.Count() < 2)
                return false;
            if (radiusBytes.Count() < 2)
                return false;

            // Velocity and Radius Bytes are sent in high bit first
            return SendCommand(137, velocityBytes[1], velocityBytes[0], radiusBytes[1], radiusBytes[0]);
        }

        #endregion

        #region

        public bool SetLed(int color, int intensity)
        {
            color.RestrictToRange(0, 255);
            intensity.RestrictToRange(0, 255);

            return SendCommand(139, 4, (byte) color, (byte) intensity);
        }

        #endregion

        #region Sensors

        public byte GetInfo(Packet toFetch)
        {
            SendCommand(142, (byte)toFetch);

            return _roomba.ReadResponse();
        }

        #endregion

        #region Private Methods

        private bool SendCommand(params byte[] commandBytes)
        {
            var success = _roomba.SendCommand(commandBytes);

            if (!success)
            {
                _currentMode = OperationMode.Off;
            }

            return success;
        }

        #endregion
    }

    public static class ExtensionMethods
    {
        public static int RestrictToRange(this int value, int lower, int higher)
        {
            if (value < lower)
                value = lower;

            if (value > higher)
                value = higher;

            return value;
        }

        public static byte[] ToBytes(this int value)
        {
            return BitConverter.GetBytes(value);
        }

        public static string ToHex(this byte value)
        {
            return BitConverter.ToString(new byte[] { value });
        }

        public static byte TwosCompliment(this byte value)
        {
            value = (byte)~value;
            return (byte)(value + 1);
        }
    }
}
