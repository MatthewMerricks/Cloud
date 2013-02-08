﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.TestClasses
{
    public class AppTestClasses
    {
        public const string WriteDefaultValues = "WriteDefaultValues";
        public CloudSDK_InputParams InputParams { get; set; }

        public AppTestClasses()
        {
            BeginGetParameters();
        }

        protected void BeginGetParameters()
        {
            RunWithDefaults();
            SetSilence();
        }

        protected void RunWithDefaults()
        {
            Console.WriteLine("Would You Like To Run The App with Preset Default Values  <true/false>? ");
            string inputValue = Console.ReadLine();
            bool useDefaults = false;
            Boolean.TryParse(inputValue, out useDefaults);
            if (useDefaults)
            {
                InputParams = new CloudSDK_InputParams(true);
                InputParams.WriteToConsole(WriteDefaultValues);
            }
            else
                InputParams = new CloudSDK_InputParams(false);

        }

        protected void SetSilence()
        {
            Console.WriteLine("Would You Like To Run In Silent Mode   <true/false>?");
            string silentinput = Console.ReadLine();
            if (!silentinput.ToLower().Equals("yes"))
                InputParams.IsSilent = false;
            else
                InputParams.IsSilent = true;
        }
    }
}