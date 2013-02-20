﻿using CloudApiPublic;
using CloudApiPublic.Model;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.CLEventArgs
{
    public class DeleteSessionEventArgs : CLEventArgs
    {
        public InputParams ParamSet { get; set; }
        public CloudApiPublic.JsonContracts.Session Session { get; set; }
        public GenericHolder<CLError> ProcessingErrorHolder { get; set; }



        public DeleteSessionEventArgs() { }

        public DeleteSessionEventArgs(DeleteSessionEventArgs inputArgs)
        {
            this.ParamSet = inputArgs.ParamSet;
            this.Creds = inputArgs.Creds;
            this.ProcessingErrorHolder = inputArgs.ProcessingErrorHolder;
            this.Session = inputArgs.Session;
        }
    }
}
