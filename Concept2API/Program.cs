using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Concept2API
{
    class Program
    {
        static private List<Concept2Device> devices = new List<Concept2Device>();

        /* Initialise */
        static void Main()
        {
            //Initialise connection(s)
            ushort deviceCount = Concept2Device.Initialize("Concept2 Performance Monitor 5 (PM5)");
            Console.Title = "Monitoring " + deviceCount + " Concept2 devices!";

            //Start up API(s)
            for (ushort i = 0; i < deviceCount; i++)
            {
                devices.Add(new Concept2Device(i));
                devices[i].Reset();
            }

            //Monitor data changes, and display
            while (true)
            {
                Thread.Sleep(80);
                Console.Clear();
                for (int i = 0; i < devices.Count; i++)
                {
                    devices[i].UpdateData();
                    Console.WriteLine("Device " + i + ":" +
                                      "\n    Phase: " + devices[i].GetStrokePhase() +
                                      "\n    Distance: " + devices[i].GetDistance() +
                                      "\n    Drag: " + devices[i].GetDrag() +
                                      "\n    Power: " + devices[i].GetPower() +
                                      "\n    Time: " + devices[i].GetTime());
                }
            }
        }
    }
}