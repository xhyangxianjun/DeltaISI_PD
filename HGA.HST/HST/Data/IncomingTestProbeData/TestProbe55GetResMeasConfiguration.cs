﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seagate.AAS.HGA.HST.Data.IncomingTestProbeData
{
    public unsafe struct TestProbe55GetResMeasConfiguration
    {
        public byte StartOfMessage;
        public byte MessageSize;
        public byte MessageType;
        public byte MessageID;
        public byte Status;
        public byte ErrorCode;

        // Ch1 bias current
        public byte Ch1BiasCurrentLSB;
        public byte Ch1BiasCurrentMSB;

        // Ch2 bias current
        public byte Ch2BiasCurrentLSB;
        public byte Ch2BiasCurrentMSB;

        // Ch3 bias current
        public byte Ch3BiasCurrentLSB;
        public byte Ch3BiasCurrentMSB;

        // Ch4 bias current
        public byte Ch4BiasCurrentLSB;
        public byte Ch4BiasCurrentMSB;

        // Ch5 bias current
        public byte Ch5BiasCurrentLSB;
        public byte Ch5BiasCurrentMSB;

        // Ch6 bias current
        public byte Ch6BiasCurrentLSB;
        public byte Ch6BiasCurrentMSB;

        // # for Average
        public byte AverageSamples;

        public static TestProbe55GetResMeasConfiguration ReadStruct(byte[] data)
        {
            fixed (byte* pb = &data[0])
            {
                return *(TestProbe55GetResMeasConfiguration*)pb;
            }
        }

        public int Ch1BiasCurrent()
        {
            byte[] array = new byte[2];
            array[0] = Ch1BiasCurrentLSB;
            array[1] = Ch1BiasCurrentMSB;

            return BitConverter.ToInt16(array, 0);
        }

        public int Ch2BiasCurrent()
        {
            byte[] array = new byte[2];
            array[0] = Ch2BiasCurrentLSB;
            array[1] = Ch2BiasCurrentMSB;

            return BitConverter.ToInt16(array, 0);
        }


        public int Ch3BiasCurrent()
        {
            byte[] array = new byte[2];
            array[0] = Ch3BiasCurrentLSB;
            array[1] = Ch3BiasCurrentMSB;

            return BitConverter.ToInt16(array, 0);
        }


        public int Ch4BiasCurrent()
        {
            byte[] array = new byte[2];
            array[0] = Ch4BiasCurrentLSB;
            array[1] = Ch4BiasCurrentMSB;

            return BitConverter.ToInt16(array, 0);
        }


        public int Ch5BiasCurrent()
        {
            byte[] array = new byte[2];
            array[0] = Ch5BiasCurrentLSB;
            array[1] = Ch5BiasCurrentMSB;

            return BitConverter.ToInt16(array, 0);
        }


        public int Ch6BiasCurrent()
        {
            byte[] array = new byte[2];
            array[0] = Ch6BiasCurrentLSB;
            array[1] = Ch6BiasCurrentMSB;

            return BitConverter.ToInt16(array, 0);
        }


        public int AverageSampling()
        {
           // byte[] array = new byte[1];
          //  array[0] = AverageSamples;           

            return (int)AverageSamples;
        }
    }
}
