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
using System.Windows;
using RateBar;
using System.Windows.Data;
using System.Linq.Expressions;

namespace CloudApiPublic.EventMessageReceiver.Status
{
    public sealed class CLStatusFileTransfer : CLStatusFileTransferBase<CLStatusFileTransfer>
    {
        // Added for calculation of current rate by samples
        // -David
        #region fields to record history of samples for current rate
        public const int RateMaxSamplesToCalculate = 50;
        public readonly List<KeyValuePair<DateTime, long>> HistorySamples = new List<KeyValuePair<DateTime, long>>();

        public const double HighestDisplayRateMultiplier = 2d;
        private const double DefaultHighestRateBitsPerSecond = 1d;
        public double HighestRateBitsPerSecond = DefaultHighestRateBitsPerSecond;
        public double InitialRateBitsPerSecondForMaxRateBar = DefaultHighestRateBitsPerSecond * HighestDisplayRateMultiplier;
        #endregion

        #region Public fields

        public bool IsDirectionUpload = false;
        public long FileSizeBytes = 0;
        public DateTime StartTime = DateTime.MinValue;
        public long SamplesTaken = 0;
        public long CumulativeBytesTransferred = 0;
        public DateTime CurrentSampleTime = DateTime.MinValue;
        public Double TransferRateBytesPerSecondAtCurrentSample = 0d;
        public bool IsComplete = false;

        #endregion

        #region Fixed bindable properties

        public override string CloudRelativePath { get; set; }

        #endregion

        #region Variable bindable properties

        /// <summary>
        /// Sets and gets the StatusGraph property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        private RateGraph _statusGraph;
        public override RateGraph StatusGraph
        {
            get { return _statusGraph; }
        }

        /// <summary>
        /// Sets and gets the Visibility property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public override Visibility Visibility
        {
            get { return Visibility.Visible; }
        }

        /// <summary>
        /// Sets and gets the DisplayRateAtCurrentSample property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public override Double DisplayRateAtCurrentSample
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
        private static string DisplayRateAtCurrentSampleName = ((MemberExpression)((Expression<Func<CLStatusFileTransfer, double>>)(parent => parent.DisplayRateAtCurrentSample)).Body).Member.Name;
        private Double _displayRateAtCurrentSample = 0d;

        /// <summary>
        /// Sets and gets the PercentComplete property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        private Double _percentComplete = 0d;
        public override Double PercentComplete
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
        /// Sets and gets the DisplayFileSize property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        private string _displayFileSize = String.Empty;
        public override string DisplayFileSize
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
        public override string DisplayTimeLeft
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
        public override string DisplayElapsedTime
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

        public void InitializeRateGraph()
        {
            _statusGraph = new RateGraph()
            {
                Height = 20,
                Margin = new Thickness(3, 1, 3, 0),
                RateMaximum = 1d,
                RateMinimum = 0d,
                Maximum = 1d,
                Minimum = 0d
            };

            _statusGraph.SetBinding(RateGraph.RateProperty,
                new Binding(DisplayRateAtCurrentSampleName)
                {
                    Source = this
                });

            _statusGraph.SetBinding(RateGraph.ValueProperty,
                new Binding(PercentCompleteName)
                {
                    Source = this
                });
        }
    }
}