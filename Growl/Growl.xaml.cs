//
//  Growl.xaml.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using CloudApiPrivate.EventMessageReceiver;
using CloudApiPrivate.Model.Settings;
using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using win_client.Common;

namespace win_client.Growl
{
    /// <summary>
    /// Interaction logic for Growl.xaml, which is a view which can "pop up" above the taskbar icon for display messages
    /// </summary>
    public partial class Growl : GrowlBase
    {
        // private enum to define the state of the static Growl and it's attachment to EventMessageReceiver changes
        private enum RunningState
        {
            NeverStarted,
            Running,
            Stopped
        }
        private static readonly GenericHolder<RunningState> WasShutDown = new GenericHolder<RunningState>(RunningState.NeverStarted);

        private static readonly EventMessageReceiver Receiver = EventMessageReceiver.Instance;

        public static CLError StartGrowlService()
        {
            try
            {
                lock (WasShutDown)
                {
                    if (WasShutDown.Value != RunningState.NeverStarted)
                    {
                        throw new Exception("GrowlService was already started");
                    }

                    Receiver.PropertyChanged += Receiver_PropertyChanged;

                    WasShutDown.Value = RunningState.Running;
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                return ex;
            }
            return null;
        }

        private static void Receiver_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName ==
                ((global::System.Linq.Expressions.MemberExpression)((global::System.Linq.Expressions.Expression<Func<EventMessageReceiver, bool>>)(parent => parent.GrowlVisible)).Body).Member.Name)
            {
                lock (OpenBalloon)
                {
                    if (OpenBalloon.Value != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke((Action<CLGrowlNotification>)(closeNotification =>
                            {
                                closeNotification.TriggerClose();
                            }),
                            OpenBalloon.Value);
                    }

                    if (Receiver.GrowlVisible)
                    {
                        OpenBalloon.Value = new CLGrowlNotification(null,
                                PopupAnimation.Slide,
                                null);

                        Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() =>
                            {
                                lock (OpenBalloon)
                                {
                                    if (OpenBalloon.Value != null
                                        && !OpenBalloon.Value.WasClosed)
                                    {
                                        OpenBalloon.Value.WpfControl = new Growl();
                                        CLAppMessages.Message_GrowlSystemTrayNotification.Send(OpenBalloon.Value);
                                    }
                                }
                            }));

                    }
                    else
                    {
                        OpenBalloon.Value = null;
                    }
                }
            }
        }
        private static readonly GenericHolder<CLGrowlNotification> OpenBalloon = new GenericHolder<CLGrowlNotification>(null);

        public static CLError ShutdownGrowlService()
        {
            try
            {
                lock (WasShutDown)
                {
                    switch (WasShutDown.Value)
                    {
                        case RunningState.NeverStarted:
                            Receiver.Dispose();
                            break;
                        case RunningState.Running:
                            lock (OpenBalloon)
                            {
                                Receiver.PropertyChanged -= Receiver_PropertyChanged;
                                if (OpenBalloon.Value != null)
                                {
                                    OpenBalloon.Value.TriggerClose();
                                    OpenBalloon.Value = null;
                                }
                                Receiver.Dispose();
                            }
                            break;
                        default:
                        //case RunningState.Stopped:
                            throw new Exception("GrowlService was already stopped");
                    }

                    WasShutDown.Value = RunningState.Stopped;
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                return ex;
            }
            return null;
        }

        public Growl()
        {
            InitializeComponent();
        }
    }
}