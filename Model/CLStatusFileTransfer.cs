//
//  CLStatusFileTransfer.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace win_client.Model
{
    public sealed class CLStatusFileTransfer
    {
        public bool IsDirectionUpload { get; set; }
        public string CloudRelativePath { get; set; }
        public long FileSizeBytes { get; set; }
        public long SamplesTaken { get; set; }
        public long BytesTransferedAtCurrentSample { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime CurrentSampleTime { get; set; }
        public Double RateAtCurrentSample { get; set; }     // 0.0 to 1.0
        public Double PercentComplete { get; set; }         // 0 to 1.0
        public bool IsComplete { get; set; }
    }
}
