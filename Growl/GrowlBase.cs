//
//  GrowlBase.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using CloudApiPrivate.EventMessageReceiver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace win_client.Growl
{
    public class GrowlBase : UserControl
    {
        private static Func<object, object, bool> BuildComparer(object comparand)
        {
            Type comparisonType = comparand == null ? typeof(object) : comparand.GetType();
            global::System.Linq.Expressions.ParameterExpression compareValue = global::System.Linq.Expressions.Expression.Parameter(typeof(object), "compareValue");
            global::System.Linq.Expressions.ParameterExpression bindingArg = global::System.Linq.Expressions.Expression.Parameter(typeof(object), "bindingArg");
            global::System.Linq.Expressions.UnaryExpression convertValue = global::System.Linq.Expressions.Expression.Convert(compareValue, comparisonType);
            global::System.Linq.Expressions.UnaryExpression convertBindingArg = global::System.Linq.Expressions.Expression.Convert(bindingArg, comparisonType);
            global::System.Linq.Expressions.BinaryExpression testEquality = global::System.Linq.Expressions.Expression.Equal(convertBindingArg, convertValue);
            return global::System.Linq.Expressions.Expression.Lambda<Func<object, object, bool>>(testEquality, compareValue, bindingArg).Compile();
        }

        public GrowlBase()
        {
            base.Loaded += GrowlBaseBase_Loaded;
        }

        private void GrowlBaseBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (_fireOnLoad != null)
            {
                while (_fireOnLoad.Count > 0)
                {
                    RaiseEvent(_fireOnLoad.Dequeue());
                }
            }
        }

        private Queue<RoutedEventArgs> FireOnLoad
        {
            get
            {
                return _fireOnLoad ?? (_fireOnLoad =
                    new Queue<RoutedEventArgs>());
            }
        }
        private Queue<RoutedEventArgs> _fireOnLoad = null;

        #region open status command
        public static readonly DependencyProperty OpenStatusCommandProperty = DependencyProperty.Register(
            "OpenStatusCommand", typeof(ICommand), typeof(GrowlBase), new PropertyMetadata(null));

        public ICommand OpenStatusCommand
        {   
            get
            {
                return (ICommand)this.GetValue(OpenStatusCommandProperty);
            }
            set
            {
                this.SetValue(OpenStatusCommandProperty, value);
            }
        }
        #endregion

        #region turn opaque
        private BindingEvaluator turnOpaqueEvaluator = null;

        public static readonly DependencyProperty TurnOpaqueBindingProperty = DependencyProperty.Register(
            "TurnOpaqueBinding", typeof(BindingAndTriggerValue), typeof(GrowlBase), new PropertyMetadata(null, new PropertyChangedCallback(TurnOpaqueBindingChanged)));

        private static void TurnOpaqueBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                ((GrowlBase)sender).RemoveTurnOpaqueBinding();
            }

            ((GrowlBase)sender).ApplyTurnOpaqueBinding((BindingAndTriggerValue)e.NewValue);
        }

        private void RemoveTurnOpaqueBinding()
        {
            if (turnOpaqueEvaluator != null)
            {
                turnOpaqueEvaluator.RemoveBinding();
                turnOpaqueEvaluator = null;
            }
        }

        private void ApplyTurnOpaqueBinding(BindingAndTriggerValue toAdd)
        {
            if (toAdd != null)
            {
                Func<object, object, bool> runComparison = BuildComparer(toAdd.Value);

                Action<DependencyPropertyChangedEventArgs> onChanged = (bindingE) =>
                    {
                        if (runComparison(toAdd.Value, bindingE.NewValue))
                        {
                            this.RaiseTurnOpaqueChanged();
                        }
                    };

                this.turnOpaqueEvaluator = new BindingEvaluator(toAdd.Binding, onChanged);

                if (BindingEvaluator.Default.Instance != this.turnOpaqueEvaluator.Result)
                {
                    onChanged(new DependencyPropertyChangedEventArgs(BindingEvaluator.ResultProperty,
                        BindingEvaluator.Default.Instance,
                        this.turnOpaqueEvaluator.Result));
                }
            }
        }

        public BindingAndTriggerValue TurnOpaqueBinding
        {
            get
            {
                return (BindingAndTriggerValue)this.GetValue(TurnOpaqueBindingProperty);
            }
            set
            {
                this.SetValue(TurnOpaqueBindingProperty, value);
            }
        }

        // Create a custom routed event by first registering a RoutedEventID 
        // This event uses the bubbling routing strategy 
        public static readonly RoutedEvent TurnOpaqueChangedEvent = EventManager.RegisterRoutedEvent(
            "TurnOpaqueChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GrowlBase));

        // Provide CLR accessors for the event 
        public event RoutedEventHandler TurnOpaqueChanged
        {
            add { AddHandler(TurnOpaqueChangedEvent, value); }
            remove { RemoveHandler(TurnOpaqueChangedEvent, value); }
        }

        // This method raises the TurnOpaqueChanged event 
        private void RaiseTurnOpaqueChanged()
        {
            RoutedEventArgs toRaise = new RoutedEventArgs(GrowlBase.TurnOpaqueChangedEvent);
            if (base.IsLoaded)
            {
                RaiseEvent(toRaise);
            }
            else
            {
                FireOnLoad.Enqueue(toRaise);
            }
        }
        #endregion

        #region stop turning opaque
        private BindingEvaluator stopTurningOpaqueEvaluator = null;

        public static readonly DependencyProperty StopTurningOpaqueBindingProperty = DependencyProperty.Register(
            "StopTurningOpaqueBinding", typeof(BindingAndTriggerValue), typeof(GrowlBase), new PropertyMetadata(null, new PropertyChangedCallback(StopTurningOpaqueBindingChanged)));

        private static void StopTurningOpaqueBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                ((GrowlBase)sender).RemoveStopTurningOpaqueBinding();
            }

            ((GrowlBase)sender).ApplyStopTurningOpaqueBinding((BindingAndTriggerValue)e.NewValue);
        }

        private void RemoveStopTurningOpaqueBinding()
        {
            if (stopTurningOpaqueEvaluator != null)
            {
                stopTurningOpaqueEvaluator.RemoveBinding();
                stopTurningOpaqueEvaluator = null;
            }
        }

        private void ApplyStopTurningOpaqueBinding(BindingAndTriggerValue toAdd)
        {
            if (toAdd != null)
            {
                Func<object, object, bool> runComparison = BuildComparer(toAdd.Value);

                Action<DependencyPropertyChangedEventArgs> onChanged = (bindingE) =>
                {
                    if (runComparison(toAdd.Value, bindingE.NewValue))
                    {
                        this.RaiseStopTurningOpaqueChanged();
                    }
                };

                this.stopTurningOpaqueEvaluator = new BindingEvaluator(toAdd.Binding, onChanged);

                if (BindingEvaluator.Default.Instance != this.stopTurningOpaqueEvaluator.Result)
                {
                    onChanged(new DependencyPropertyChangedEventArgs(BindingEvaluator.ResultProperty,
                        BindingEvaluator.Default.Instance,
                        this.stopTurningOpaqueEvaluator.Result));
                }
            }
        }

        public BindingAndTriggerValue StopTurningOpaqueBinding
        {
            get
            {
                return (BindingAndTriggerValue)this.GetValue(StopTurningOpaqueBindingProperty);
            }
            set
            {
                this.SetValue(StopTurningOpaqueBindingProperty, value);
            }
        }

        // Create a custom routed event by first registering a RoutedEventID 
        // This event uses the bubbling routing strategy 
        public static readonly RoutedEvent StopTurningOpaqueChangedEvent = EventManager.RegisterRoutedEvent(
            "StopTurningOpaqueChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GrowlBase));

        // Provide CLR accessors for the event 
        public event RoutedEventHandler StopTurningOpaqueChanged
        {
            add { AddHandler(StopTurningOpaqueChangedEvent, value); }
            remove { RemoveHandler(StopTurningOpaqueChangedEvent, value); }
        }

        // This method raises the StopTurningOpaqueChanged event 
        private void RaiseStopTurningOpaqueChanged()
        {
            RoutedEventArgs toRaise = new RoutedEventArgs(GrowlBase.StopTurningOpaqueChangedEvent);
            if (base.IsLoaded)
            {
                RaiseEvent(toRaise);
            }
            else
            {
                FireOnLoad.Enqueue(toRaise);
            }
        }
        #endregion

        #region turn translucent
        private BindingEvaluator turnTranslucentEvaluator = null;

        public static readonly DependencyProperty TurnTranslucentBindingProperty = DependencyProperty.Register(
            "TurnTranslucentBinding", typeof(BindingAndTriggerValue), typeof(GrowlBase), new PropertyMetadata(null, new PropertyChangedCallback(TurnTranslucentBindingChanged)));

        private static void TurnTranslucentBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                ((GrowlBase)sender).RemoveTurnTranslucentBinding();
            }

            ((GrowlBase)sender).ApplyTurnTranslucentBinding((BindingAndTriggerValue)e.NewValue);
        }

        private void RemoveTurnTranslucentBinding()
        {
            if (turnTranslucentEvaluator != null)
            {
                turnTranslucentEvaluator.RemoveBinding();
                turnTranslucentEvaluator = null;
            }
        }

        private void ApplyTurnTranslucentBinding(BindingAndTriggerValue toAdd)
        {
            if (toAdd != null)
            {
                Func<object, object, bool> runComparison = BuildComparer(toAdd.Value);

                Action<DependencyPropertyChangedEventArgs> onChanged = (bindingE) =>
                {
                    if (runComparison(toAdd.Value, bindingE.NewValue))
                    {
                        this.RaiseTurnTranslucentChanged();
                    }
                };

                this.turnTranslucentEvaluator = new BindingEvaluator(toAdd.Binding, onChanged);

                if (BindingEvaluator.Default.Instance != this.turnTranslucentEvaluator.Result)
                {
                    onChanged(new DependencyPropertyChangedEventArgs(BindingEvaluator.ResultProperty,
                        BindingEvaluator.Default.Instance,
                        this.turnTranslucentEvaluator.Result));
                }
            }
        }

        public BindingAndTriggerValue TurnTranslucentBinding
        {
            get
            {
                return (BindingAndTriggerValue)this.GetValue(TurnTranslucentBindingProperty);
            }
            set
            {
                this.SetValue(TurnTranslucentBindingProperty, value);
            }
        }

        // Create a custom routed event by first registering a RoutedEventID 
        // This event uses the bubbling routing strategy 
        public static readonly RoutedEvent TurnTranslucentChangedEvent = EventManager.RegisterRoutedEvent(
            "TurnTranslucentChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GrowlBase));

        // Provide CLR accessors for the event 
        public event RoutedEventHandler TurnTranslucentChanged
        {
            add { AddHandler(TurnTranslucentChangedEvent, value); }
            remove { RemoveHandler(TurnTranslucentChangedEvent, value); }
        }

        // This method raises the TurnTranslucentChanged event 
        private void RaiseTurnTranslucentChanged()
        {
            RoutedEventArgs toRaise = new RoutedEventArgs(GrowlBase.TurnTranslucentChangedEvent);
            if (base.IsLoaded)
            {
                RaiseEvent(toRaise);
            }
            else
            {
                FireOnLoad.Enqueue(toRaise);
            }
        }
        #endregion

        #region stop turning opaque
        private BindingEvaluator stopTurningTranslucentEvaluator = null;

        public static readonly DependencyProperty StopTurningTranslucentBindingProperty = DependencyProperty.Register(
            "StopTurningTranslucentBinding", typeof(BindingAndTriggerValue), typeof(GrowlBase), new PropertyMetadata(null, new PropertyChangedCallback(StopTurningTranslucentBindingChanged)));

        private static void StopTurningTranslucentBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                ((GrowlBase)sender).RemoveStopTurningTranslucentBinding();
            }

            ((GrowlBase)sender).ApplyStopTurningTranslucentBinding((BindingAndTriggerValue)e.NewValue);
        }

        private void RemoveStopTurningTranslucentBinding()
        {
            if (stopTurningTranslucentEvaluator != null)
            {
                stopTurningTranslucentEvaluator.RemoveBinding();
                stopTurningTranslucentEvaluator = null;
            }
        }

        private void ApplyStopTurningTranslucentBinding(BindingAndTriggerValue toAdd)
        {
            if (toAdd != null)
            {
                Func<object, object, bool> runComparison = BuildComparer(toAdd.Value);

                Action<DependencyPropertyChangedEventArgs> onChanged = (bindingE) =>
                {
                    if (runComparison(toAdd.Value, bindingE.NewValue))
                    {
                        this.RaiseStopTurningTranslucentChanged();
                    }
                };

                this.stopTurningTranslucentEvaluator = new BindingEvaluator(toAdd.Binding, onChanged);

                if (BindingEvaluator.Default.Instance != this.stopTurningTranslucentEvaluator.Result)
                {
                    onChanged(new DependencyPropertyChangedEventArgs(BindingEvaluator.ResultProperty,
                        BindingEvaluator.Default.Instance,
                        this.stopTurningTranslucentEvaluator.Result));
                }
            }
        }

        public BindingAndTriggerValue StopTurningTranslucentBinding
        {
            get
            {
                return (BindingAndTriggerValue)this.GetValue(StopTurningTranslucentBindingProperty);
            }
            set
            {
                this.SetValue(StopTurningTranslucentBindingProperty, value);
            }
        }

        // Create a custom routed event by first registering a RoutedEventID 
        // This event uses the bubbling routing strategy 
        public static readonly RoutedEvent StopTurningTranslucentChangedEvent = EventManager.RegisterRoutedEvent(
            "StopTurningTranslucentChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GrowlBase));

        // Provide CLR accessors for the event 
        public event RoutedEventHandler StopTurningTranslucentChanged
        {
            add { AddHandler(StopTurningTranslucentChangedEvent, value); }
            remove { RemoveHandler(StopTurningTranslucentChangedEvent, value); }
        }

        // This method raises the StopTurningTranslucentChanged event 
        private void RaiseStopTurningTranslucentChanged()
        {
            RoutedEventArgs toRaise = new RoutedEventArgs(GrowlBase.StopTurningTranslucentChangedEvent);
            if (base.IsLoaded)
            {
                RaiseEvent(toRaise);
            }
            else
            {
                FireOnLoad.Enqueue(toRaise);
            }
        }
        #endregion

        #region stay completely opaque
        private BindingEvaluator stayOpaqueEvaluator = null;

        public static readonly DependencyProperty StayOpaqueBindingProperty = DependencyProperty.Register(
            "StayOpaqueBinding", typeof(BindingAndTriggerValue), typeof(GrowlBase), new PropertyMetadata(null, new PropertyChangedCallback(StayOpaqueBindingChanged)));

        private static void StayOpaqueBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                ((GrowlBase)sender).RemoveStayOpaqueBinding();
            }

            ((GrowlBase)sender).ApplyStayOpaqueBinding((BindingAndTriggerValue)e.NewValue);
        }

        private void RemoveStayOpaqueBinding()
        {
            if (stayOpaqueEvaluator != null)
            {
                stayOpaqueEvaluator.RemoveBinding();
                stayOpaqueEvaluator = null;
            }
        }

        private void ApplyStayOpaqueBinding(BindingAndTriggerValue toAdd)
        {
            if (toAdd != null)
            {
                Func<object, object, bool> runComparison = BuildComparer(toAdd.Value);

                Action<DependencyPropertyChangedEventArgs> onChanged = (bindingE) =>
                {
                    if (runComparison(toAdd.Value, bindingE.NewValue))
                    {
                        this.RaiseStayOpaqueChanged();
                    }
                };

                this.stayOpaqueEvaluator = new BindingEvaluator(toAdd.Binding, onChanged);

                if (BindingEvaluator.Default.Instance != this.stayOpaqueEvaluator.Result)
                {
                    onChanged(new DependencyPropertyChangedEventArgs(BindingEvaluator.ResultProperty,
                        BindingEvaluator.Default.Instance,
                        this.stayOpaqueEvaluator.Result));
                }
            }
        }

        public BindingAndTriggerValue StayOpaqueBinding
        {
            get
            {
                return (BindingAndTriggerValue)this.GetValue(StayOpaqueBindingProperty);
            }
            set
            {
                this.SetValue(StayOpaqueBindingProperty, value);
            }
        }

        // Create a custom routed event by first registering a RoutedEventID 
        // This event uses the bubbling routing strategy 
        public static readonly RoutedEvent StayOpaqueChangedEvent = EventManager.RegisterRoutedEvent(
            "StayOpaqueChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GrowlBase));

        // Provide CLR accessors for the event 
        public event RoutedEventHandler StayOpaqueChanged
        {
            add { AddHandler(StayOpaqueChangedEvent, value); }
            remove { RemoveHandler(StayOpaqueChangedEvent, value); }
        }

        // This method raises the StayOpaqueChanged event 
        private void RaiseStayOpaqueChanged()
        {
            RoutedEventArgs toRaise = new RoutedEventArgs(GrowlBase.StayOpaqueChangedEvent);
            if (base.IsLoaded)
            {
                RaiseEvent(toRaise);
            }
            else
            {
                FireOnLoad.Enqueue(toRaise);
            }
        }
        #endregion
    }
}