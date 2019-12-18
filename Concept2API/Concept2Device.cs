using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace Concept2API
{
    /* Possible stroke phases */
    public enum StrokePhase
    {
        Idle = 0, //Not yet started pulling, or disconnected
        Catch = 1, //Finished all previously active movement
        Drive = 2, //User is pulling
        Dwell = 3, //User just finished pulling
        Recovery = 4 //Pulling finished, spooling down
    }

    /* Commands for ERG */
    public enum CSAFE : uint
    {
        CSAFE_GETSTATUS_CMD = 0x80,
        CSAFE_RESET_CMD = 0x81,
        CSAFE_GOIDLE_CMD = 0x82,
        CSAFE_GOHAVEID_CMD = 0x83,
        CSAFE_GOINUSE_CMD = 0x85,
        CSAFE_GOFINISHED_CMD = 0x86,
        CSAFE_GOREADY_CMD = 0x87,
        CSAFE_BADID_CMD = 0x88,
        CSAFE_GETVERSION_CMD = 0x91,
        CSAFE_GETID_CMD = 0x92,
        CSAFE_GETUNITS_CMD = 0x93,
        CSAFE_GETSERIAL_CMD = 0x94,
        CSAFE_GETLIST_CMD = 0x98,
        CSAFE_GETUTILIZATION_CMD = 0x99,
        CSAFE_GETMOTORCURRENT_CMD = 0x9A,
        CSAFE_GETODOMETER_CMD = 0x9B,
        CSAFE_GETERRORCODE_CMD = 0x9C,
        CSAFE_GETSERVICECODE_CMD = 0x9D,
        CSAFE_GETUSERCFG1_CMD = 0x9E,
        CSAFE_GETUSERCFG2_CMD = 0x9F,
        CSAFE_GETTWORK_CMD = 0xA0,
        CSAFE_GETHORIZONTAL_CMD = 0xA1,
        CSAFE_GETVERTICAL_CMD = 0xA2,
        CSAFE_GETCALORIES_CMD = 0xA3,
        CSAFE_GETPROGRAM_CMD = 0xA4,
        CSAFE_GETSPEED_CMD = 0xA5,
        CSAFE_GETPACE_CMD = 0xA6,
        CSAFE_GETCADENCE_CMD = 0xA7,
        CSAFE_GETGRADE_CMD = 0xA8,
        CSAFE_GETGEAR_CMD = 0xA9,
        CSAFE_GETUPLIST_CMD = 0xAA,
        CSAFE_GETUSERINFO_CMD = 0xAB,
        CSAFE_GETTORQUE_CMD = 0xAC,
        CSAFE_GETHRCUR_CMD = 0xB0,
        CSAFE_GETHRTZONE_CMD = 0xB2,
        CSAFE_GETMETS_CMD = 0xB3,
        CSAFE_GETPOWER_CMD = 0xB4,
        CSAFE_GETHRAVG_CMD = 0xB5,
        CSAFE_GETHRMAX_CMD = 0xB6,
        CSAFE_GETUSERDATA1_CMD = 0xBE,
        CSAFE_GETUSERDATA2_CMD = 0xBF,
        CSAFE_SETUSERCFG1_CMD = 0x1A,
        CSAFE_SETTWORK_CMD = 0x20,
        CSAFE_SETHORIZONTAL_CMD = 0x21,
        CSAFE_SETPROGRAM_CMD = 0x24,
        CSAFE_SETTARGETHR_CMD = 0x30,
        CSAFE_PM_GET_WORKDISTANCE = 0xA3,
        CSAFE_PM_GET_WORKTIME = 0xA0,
        CSAFE_PM_SET_SPLITDURATION = 0x05,
        CSAFE_PM_GET_FORCEPLOTDATA = 0x6B,
        CSAFE_PM_GET_DRAGFACTOR = 0xC1,
        CSAFE_PM_GET_STROKESTATE = 0xBF,
        CSAFE_UNITS_METER = 0x24
    }

    public class Concept2Device
    {
        /* Initialise the connection */
        private static bool staticInitialized = false;
        private static ushort pm3Count = 0;
        public static ushort Initialize(string name)
        {
            if (Concept2Device.staticInitialized)
                return 0;

            short errorCode = tkcmdsetDDI_init();

            if (0 == errorCode)
            {
                //Init CSAFE protocol
                errorCode = tkcmdsetCSAFE_init_protocol(1000);

                if (0 == errorCode)
                {
                    errorCode = tkcmdsetDDI_discover_pm3s(name, 0, ref Concept2Device.pm3Count);

                    if (0 == errorCode)
                    {
                        if (Concept2Device.pm3Count > 0)
                        {
                            staticInitialized = true;
                            return Concept2Device.pm3Count;
                        }
                        else
                        {
                            Console.WriteLine("Error initializing.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error initializing.");
                    }
                }
            }

            //Some error occurred.
            return 0;
        }

        private ushort deviceNumber;
        public Concept2Device(ushort index)
        {
            deviceNumber = index;
        }

        /* Drag data */
        private int iDragFactor = 0;
        public int GetDrag()
        {
            return iDragFactor;
        }

        /* Distance data */
        private int iWorkDistance = 0;
        public int GetDistance()
        {
            return iWorkDistance;
        }

        /* Time data */
        private double dWorkTime = 0;
        public double GetTime()
        {
            return dWorkTime;
        }

        /* Power data */
        private int iPower = 0;
        public int GetPower()
        {
            return iPower;
        }

        /* Stroke phase data */
        private StrokePhase eStrokePhase = StrokePhase.Idle;
        public StrokePhase GetStrokePhase()
        {
            return eStrokePhase;
        }

        /* Update our tracked data from the connection */
        public void UpdateData()
        {
            uint[] cmd_data = new uint[64];
            uint[] rsp_data = new uint[1024];
            ushort rsp_data_size = 120;
            ushort cmd_data_size = 0;

            //Header and number of extension commands.
            cmd_data[cmd_data_size++] = (uint)CSAFE.CSAFE_SETUSERCFG1_CMD;
            cmd_data[cmd_data_size++] = 0x04;

            //Three PM3 extension commands.
            cmd_data[cmd_data_size++] = (uint)CSAFE.CSAFE_PM_GET_DRAGFACTOR;
            cmd_data[cmd_data_size++] = (uint)CSAFE.CSAFE_PM_GET_WORKDISTANCE;
            cmd_data[cmd_data_size++] = (uint)CSAFE.CSAFE_PM_GET_WORKTIME;
            cmd_data[cmd_data_size++] = (uint)CSAFE.CSAFE_PM_GET_STROKESTATE;
            //Call more commands here to get more data, but remember to update the count above.

            //Standard commands.
            cmd_data[cmd_data_size++] = (uint)CSAFE.CSAFE_GETPOWER_CMD;

            //Run command
            short ecode = tkcmdsetCSAFE_command(deviceNumber, cmd_data_size, cmd_data, ref rsp_data_size, rsp_data);
            uint currentbyte = 0;
            uint datalength = 0;
            if (rsp_data[currentbyte] == (uint)CSAFE.CSAFE_SETUSERCFG1_CMD)
            {
                currentbyte += 2;
            }

            //Update drag
            if (rsp_data[currentbyte] == (uint)CSAFE.CSAFE_PM_GET_DRAGFACTOR)
            {
                currentbyte++;
                datalength = rsp_data[currentbyte];
                currentbyte++;

                iDragFactor = (int)rsp_data[currentbyte];

                currentbyte += datalength;
            }

            //Update distance
            if (rsp_data[currentbyte] == (uint)CSAFE.CSAFE_PM_GET_WORKDISTANCE)
            {
                currentbyte++;
                datalength = rsp_data[currentbyte];
                currentbyte++;

                iWorkDistance = (int)((rsp_data[currentbyte] + (rsp_data[currentbyte + 1] << 8) + (rsp_data[currentbyte + 2] << 16) + (rsp_data[currentbyte + 3] << 24)) / 10);

                currentbyte += datalength;
            }

            //Update time
            if (rsp_data[currentbyte] == (uint)CSAFE.CSAFE_PM_GET_WORKTIME)
            {
                currentbyte++;
                datalength = rsp_data[currentbyte];
                currentbyte++;

                if (datalength == 5)
                {
                    uint timeInSeconds = (rsp_data[currentbyte] + (rsp_data[currentbyte + 1] << 8) + (rsp_data[currentbyte + 2] << 16) + (rsp_data[currentbyte + 3] << 24)) / 100;
                    uint fraction = rsp_data[currentbyte + 4];

                    dWorkTime = timeInSeconds + (fraction / 100.0);
                }

                currentbyte += datalength;
            }

            //Update stroke state
            if (rsp_data[currentbyte] == (uint)CSAFE.CSAFE_PM_GET_STROKESTATE)
            {
                currentbyte++;
                datalength = rsp_data[currentbyte];
                currentbyte++;

                switch (rsp_data[currentbyte])
                {
                    case 0:
                    case 1:
                        eStrokePhase = StrokePhase.Catch;
                        break;
                    case 2:
                        eStrokePhase = StrokePhase.Drive;
                        break;
                    case 3:
                        eStrokePhase = StrokePhase.Dwell;
                        break;
                    case 4:
                        eStrokePhase = StrokePhase.Recovery;
                        break;
                }

                currentbyte += datalength;
            }

            //Update power
            if (rsp_data[currentbyte] == (uint)CSAFE.CSAFE_GETPOWER_CMD)
            {
                currentbyte++;
                datalength = rsp_data[currentbyte];
                currentbyte++;

                iPower = (int)(rsp_data[currentbyte] + (rsp_data[currentbyte + 1] << 8));

                currentbyte += datalength;
            }
        }

        /* Reset the connection */
        public void Reset()
        {
            uint[] cmd_data = new uint[64];
            ushort cmd_data_size;
            uint[] rsp_data = new uint[64];
            ushort rsp_data_size = 0;

            cmd_data_size = 0;

            //Reset.
            cmd_data[cmd_data_size++] = (uint)CSAFE.CSAFE_GOFINISHED_CMD;
            cmd_data[cmd_data_size++] = (uint)CSAFE.CSAFE_GOIDLE_CMD;

            //Start.
            cmd_data[cmd_data_size++] = (uint)CSAFE.CSAFE_GOHAVEID_CMD;
            cmd_data[cmd_data_size++] = (uint)CSAFE.CSAFE_GOINUSE_CMD;

            tkcmdsetCSAFE_command(deviceNumber, cmd_data_size, cmd_data, ref rsp_data_size, rsp_data);
        }

        [DllImport("PM3DDICP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern short tkcmdsetDDI_init();

        [DllImport("PM3DDICP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern short tkcmdsetDDI_discover_pm3s(
           string product_name,
           ushort starting_address,
           ref ushort num_units);

        [DllImport("PM3CsafeCP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern short tkcmdsetCSAFE_init_protocol(ushort timeout);

        [DllImport("PM3CsafeCP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern short tkcmdsetCSAFE_command(
           ushort unit_address,
           ushort cmd_data_size,
           uint[] cmd_data,
           ref ushort rsp_data_size,
           uint[] rsp_data);
    }
}