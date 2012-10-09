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
using GalaSoft.MvvmLight;
using CloudApiPrivate.Model;

namespace win_client.Model
{
    public sealed class CLStatusFileTransfer : NotifiableObject<CLStatusFileTransfer>
    {
        #region Fixed bindable properties

        public bool IsDirectionUpload { get; set; }
        public string CloudRelativePath { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime StartTime { get; set; }
        
        #endregion

        #region Variable bindable properties

        /// <summary>
        /// The <see cref="SamplesTaken" /> property's name.
        /// </summary>
        private long _samplesTaken = 0;
        public long SamplesTaken
        {
            get { return _samplesTaken; }
            set
            {
                if (_samplesTaken == value) { return; }
                _samplesTaken = value;
                NotifyPropertyChanged(parent => parent.SamplesTaken);
            }
        }

        /// <summary>
        /// Sets and gets the CumulativeBytesTransfered property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        private long _cumulativeBytesTransfered = 0;
        public long CumulativeBytesTransfered
        {
            get { return _cumulativeBytesTransfered; }
            set
            {
                if (_cumulativeBytesTransfered != value)
                {
                    _cumulativeBytesTransfered = value;
                    NotifyPropertyChanged(parent => parent.CumulativeBytesTransfered);
                }
            }
        }

        /// <summary>
        /// Sets and gets the CurrentSampleTime property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        private DateTime _currentSampleTime = DateTime.MinValue;
        public DateTime CurrentSampleTime
        {
            get { return _currentSampleTime; }
            set
            {
                if (_currentSampleTime != value)
                {
                    _currentSampleTime = value;
                    NotifyPropertyChanged(parent => parent.CurrentSampleTime);
                }
            }
        }

        /// <summary>
        /// Sets and gets the TransferRateBytesPerSecondAtCurrentSample property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        private Double _transferRateBytesPerSecondAtCurrentSample = 0.0;
        public Double TransferRateBytesPerSecondAtCurrentSample
        {
            get { return _transferRateBytesPerSecondAtCurrentSample; }
            set
            {
                if (_transferRateBytesPerSecondAtCurrentSample != value)
                {
                    _transferRateBytesPerSecondAtCurrentSample = value;
                    NotifyPropertyChanged(parent => parent.TransferRateBytesPerSecondAtCurrentSample);
                }
            }
        }

        /// <summary>
        /// Sets and gets the DisplayRateAtCurrentSample property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        private Double _displayRateAtCurrentSample = 0.0;
        public Double DisplayRateAtCurrentSample
        {
            get { return _displayRateAtCurrentSample; }
            set
            {
                if (_displayRateAtCurrentSample != value)
                {
                    _displayRateAtCurrentSample = value;
                    NotifyPropertyChanged(parent => parent.DisplayRateAtCurrentSample);
                }
            }
        }

        /// <summary>
        /// Sets and gets the PercentComplete property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        private Double _percentComplete = 0.0;
        public Double PercentComplete
        {
            get { return _percentComplete; }
            set
            {
                if (_percentComplete != value)
                {
                    _percentComplete = value;
                    NotifyPropertyChanged(parent => parent.PercentComplete);
                }
            }
        }

        /// <summary>
        /// Sets and gets the IsComplete property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        private bool _isComplete = false;
        public bool IsComplete
        {
            get { return _isComplete; }
            set
            {
                if (_isComplete != value)
                {
                    _isComplete = value;
                    NotifyPropertyChanged(parent => parent.IsComplete);
                }
            }
        }

        /// <summary>
        /// Sets and gets the DisplayFileSize property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        private string _displayFileSize = String.Empty;
        public string DisplayFileSize
        {
            get { return _displayFileSize; }
            set
            {
                if (_displayFileSize != value)
                {
                    _displayFileSize = value;
                    NotifyPropertyChanged(parent => parent.DisplayFileSize);
                }
            }
        }

        /// <summary>
        /// Sets and gets the DisplayTimeLeft property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        private string _displayTimeLeft = String.Empty;
        public string DisplayTimeLeft
        {
            get { return _displayTimeLeft; }
            set
            {
                if (_displayTimeLeft != value)
                {
                    _displayTimeLeft = value;
                    NotifyPropertyChanged(parent => parent.DisplayTimeLeft);
                }
            }
        }

        /// <summary>
        /// Sets and gets the DisplayElapsedTime property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        private string _displayElapsedTime = String.Empty;
        public string DisplayElapsedTime
        {
            get { return _displayElapsedTime; }
            set
            {
                if (_displayElapsedTime != value)
                {
                    _displayElapsedTime = value;
                    NotifyPropertyChanged(parent => parent.DisplayElapsedTime);
                }
            }
        }

        #endregion
    }
}
