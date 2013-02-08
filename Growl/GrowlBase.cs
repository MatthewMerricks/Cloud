//
//  GrowlBase.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CloudApiPublic.EventMessageReceiver;

namespace win_client.Growl
{
    /// <summary>
    /// Base class for Growl (Growl.xaml and Growl.xaml.cs) to allow DependencyProperty bindings from its XAML;
    /// the purpose of the bindings are to route data change events captured via BindingEvaluator for fading in and out to RoutedEvents for triggering EventTriggers for animation
    /// </summary>
    public class GrowlBase : UserControl
    {
        // compiles a dynamic comparison function between two objects (instead of using object's underlying compare in case simple reference comparison is not appropriate for the provided types)
        private static Func<object, object, bool> BuildComparer(object comparand)
        {
            // use the type of the comparand as the base type for comparison
            Type comparisonType = comparand == null ? typeof(object) : comparand.GetType();

            // expression set to create the code:
            // return (([type of comparand])comparand) == (([type of comparand])valueToCompare)
            global::System.Linq.Expressions.ParameterExpression compareValue = global::System.Linq.Expressions.Expression.Parameter(typeof(object), "compareValue");
            global::System.Linq.Expressions.ParameterExpression bindingArg = global::System.Linq.Expressions.Expression.Parameter(typeof(object), "bindingArg");
            global::System.Linq.Expressions.UnaryExpression convertValue = global::System.Linq.Expressions.Expression.Convert(compareValue, comparisonType);
            global::System.Linq.Expressions.UnaryExpression convertBindingArg = global::System.Linq.Expressions.Expression.Convert(bindingArg, comparisonType);
            global::System.Linq.Expressions.BinaryExpression testEquality = global::System.Linq.Expressions.Expression.Equal(convertBindingArg, convertValue);
            return global::System.Linq.Expressions.Expression.Lambda<Func<object, object, bool>>(testEquality, compareValue, bindingArg).Compile();
        }

        // constructor attaches a Loaded EventHandler to process firing any RoutedEvents which were queued before load
        public GrowlBase()
        {
            base.Loaded += GrowlBaseBase_Loaded;
        }

        // EventHandler to process firing any RoutedEvents which were queued before load
        private void GrowlBaseBase_Loaded(object sender, RoutedEventArgs e)
        {
            // if there is a queue of RoutedEvents to fire, fire them all and reset the queue
            if (_fireOnLoad != null)
            {
                Array.ForEach(_fireOnLoad.ToArray(),
                    currentToFire => RaiseEvent(currentToFire));
                _fireOnLoad = null;
            }
        }

        // retrieves or creates and retrieves a Queue of arguments to fire RoutedEvents upon Load
        private Queue<RoutedEventArgs> FireOnLoad
        {
            get
            {
                return _fireOnLoad ?? (_fireOnLoad =
                    new Queue<RoutedEventArgs>());
            }
        }
        // stores a Queue of arguments to fire RoutedEvents upon Load, defaulting to null
        private Queue<RoutedEventArgs> _fireOnLoad = null;

        #region open status command
        /// <summary>
        /// Property for OpenStatusCommand, which should be set to an action of opening the Sync Status window
        /// </summary>
        public static readonly DependencyProperty OpenStatusCommandProperty = DependencyProperty.Register(
            "OpenStatusCommand", typeof(ICommand), typeof(GrowlBase), new PropertyMetadata(null));

        /// <summary>
        /// Should be set to an action of opening the Sync Status window
        /// </summary>
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
        // define the evaluator for when the growl should fade in, defaulting to null
        private BindingEvaluator turnOpaqueEvaluator = null;

        /// <summary>
        /// Property for TurnOpaqueBinding, which should be set to a BindingAndTriggerValue condition for when the growl should fade in
        /// </summary>
        public static readonly DependencyProperty TurnOpaqueBindingProperty = DependencyProperty.Register(
            "TurnOpaqueBinding", typeof(BindingAndTriggerValue), typeof(GrowlBase), new PropertyMetadata(null, new PropertyChangedCallback(TurnOpaqueBindingChanged)));

        // handler for when the TurnOpaqueBinding is set; applies the binding
        private static void TurnOpaqueBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            // if a binding was previously set, remove it
            if (e.OldValue != null)
            {
                ((GrowlBase)sender).RemoveTurnOpaqueBinding();
            }

            // apply the new binding
            ((GrowlBase)sender).ApplyTurnOpaqueBinding((BindingAndTriggerValue)e.NewValue);
        }

        // removes the TurnOpaqueBinding if it had an evaluator
        private void RemoveTurnOpaqueBinding()
        {
            if (turnOpaqueEvaluator != null)
            {
                turnOpaqueEvaluator.RemoveBinding();
                turnOpaqueEvaluator = null;
            }
        }

        // applies the TurnOpaqueBinding
        private void ApplyTurnOpaqueBinding(BindingAndTriggerValue toAdd)
        {
            // if there is a binding to apply, then apply it
            if (toAdd != null)
            {
                // compile the binding comparison for when the data changes
                Func<object, object, bool> runComparison = BuildComparer(toAdd.Value);

                // build the data change action which checks the custom comparison to raise the RoutedEvent
                Action<DependencyPropertyChangedEventArgs> onChanged = (bindingE) =>
                    {
                        if (runComparison(toAdd.Value, bindingE.NewValue))
                        {
                            this.RaiseTurnOpaqueChanged();
                        }
                    };

                // create the BindingEvaluator with the binding comparison and change event handler
                this.turnOpaqueEvaluator = new BindingEvaluator(toAdd.Binding, onChanged);

                // if the bound value is already different from the initial, default value, then immediately fire the changed event to run the comparison
                if (BindingEvaluator.Default.Instance != this.turnOpaqueEvaluator.Result)
                {
                    onChanged(new DependencyPropertyChangedEventArgs(BindingEvaluator.ResultProperty,
                        BindingEvaluator.Default.Instance,
                        this.turnOpaqueEvaluator.Result));
                }
            }
        }

        /// <summary>
        /// Should be set to a BindingAndTriggerValue condition for when the growl should fade in, fires TurnOpaqueChanged RoutedEvent when condition is met
        /// </summary>
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
        /// <summary>
        /// Event for when growl needs to fade in
        /// </summary>
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
        // define the evaluator for when the growl should stop fading in, defaulting to null
        private BindingEvaluator stopTurningOpaqueEvaluator = null;

        /// <summary>
        /// Property for StopTurningOpaqueBinding, which should be set to a BindingAndTriggerValue condition for when the growl should stop fading in
        /// </summary>
        public static readonly DependencyProperty StopTurningOpaqueBindingProperty = DependencyProperty.Register(
            "StopTurningOpaqueBinding", typeof(BindingAndTriggerValue), typeof(GrowlBase), new PropertyMetadata(null, new PropertyChangedCallback(StopTurningOpaqueBindingChanged)));

        // handler for when the StopTurningOpaqueBinding is set; applies the binding
        private static void StopTurningOpaqueBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            // if a binding was previously set, remove it
            if (e.OldValue != null)
            {
                ((GrowlBase)sender).RemoveStopTurningOpaqueBinding();
            }

            // apply the new binding
            ((GrowlBase)sender).ApplyStopTurningOpaqueBinding((BindingAndTriggerValue)e.NewValue);
        }

        // removes the StopTurningOpaqueBinding if it had an evaluator
        private void RemoveStopTurningOpaqueBinding()
        {
            if (stopTurningOpaqueEvaluator != null)
            {
                stopTurningOpaqueEvaluator.RemoveBinding();
                stopTurningOpaqueEvaluator = null;
            }
        }

        // applies the StopTurningOpaqueBinding
        private void ApplyStopTurningOpaqueBinding(BindingAndTriggerValue toAdd)
        {
            // if there is a binding to apply, then apply it
            if (toAdd != null)
            {
                // compile the binding comparison for when the data changes
                Func<object, object, bool> runComparison = BuildComparer(toAdd.Value);

                // build the data change action which checks the custom comparison to raise the RoutedEvent
                Action<DependencyPropertyChangedEventArgs> onChanged = (bindingE) =>
                {
                    if (runComparison(toAdd.Value, bindingE.NewValue))
                    {
                        this.RaiseStopTurningOpaqueChanged();
                    }
                };

                // create the BindingEvaluator with the binding comparison and change event handler
                this.stopTurningOpaqueEvaluator = new BindingEvaluator(toAdd.Binding, onChanged);

                // if the bound value is already different from the initial, default value, then immediately fire the changed event to run the comparison
                if (BindingEvaluator.Default.Instance != this.stopTurningOpaqueEvaluator.Result)
                {
                    onChanged(new DependencyPropertyChangedEventArgs(BindingEvaluator.ResultProperty,
                        BindingEvaluator.Default.Instance,
                        this.stopTurningOpaqueEvaluator.Result));
                }
            }
        }

        /// <summary>
        /// Should be set to a BindingAndTriggerValue condition for when the growl should fade in, fires StopTurningOpaqueChanged RoutedEvent when condition is met
        /// </summary>
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
        /// <summary>
        /// Event for when growl needs to stop fading in
        /// </summary>
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
        // define the evaluator for when the growl should fade out, defaulting to null
        private BindingEvaluator turnTranslucentEvaluator = null;

        /// <summary>
        /// Property for TurnTranslucentBinding, which should be set to a BindingAndTriggerValue condition for when the growl should fade out
        /// </summary>
        public static readonly DependencyProperty TurnTranslucentBindingProperty = DependencyProperty.Register(
            "TurnTranslucentBinding", typeof(BindingAndTriggerValue), typeof(GrowlBase), new PropertyMetadata(null, new PropertyChangedCallback(TurnTranslucentBindingChanged)));

        // handler for when the TurnTranslucentBinding is set; applies the binding
        private static void TurnTranslucentBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            // if a binding was previously set, remove it
            if (e.OldValue != null)
            {
                ((GrowlBase)sender).RemoveTurnTranslucentBinding();
            }

            // apply the new binding
            ((GrowlBase)sender).ApplyTurnTranslucentBinding((BindingAndTriggerValue)e.NewValue);
        }

        // removes the TurnTranslucentBinding if it had an evaluator
        private void RemoveTurnTranslucentBinding()
        {
            if (turnTranslucentEvaluator != null)
            {
                turnTranslucentEvaluator.RemoveBinding();
                turnTranslucentEvaluator = null;
            }
        }

        // applies the TurnTranslucentBinding
        private void ApplyTurnTranslucentBinding(BindingAndTriggerValue toAdd)
        {
            // if there is a binding to apply, then apply it
            if (toAdd != null)
            {
                // compile the binding comparison for when the data changes
                Func<object, object, bool> runComparison = BuildComparer(toAdd.Value);

                // build the data change action which checks the custom comparison to raise the RoutedEvent
                Action<DependencyPropertyChangedEventArgs> onChanged = (bindingE) =>
                {
                    if (runComparison(toAdd.Value, bindingE.NewValue))
                    {
                        this.RaiseTurnTranslucentChanged();
                    }
                };

                // create the BindingEvaluator with the binding comparison and change event handler
                this.turnTranslucentEvaluator = new BindingEvaluator(toAdd.Binding, onChanged);

                // if the bound value is already different from the initial, default value, then immediately fire the changed event to run the comparison
                if (BindingEvaluator.Default.Instance != this.turnTranslucentEvaluator.Result)
                {
                    onChanged(new DependencyPropertyChangedEventArgs(BindingEvaluator.ResultProperty,
                        BindingEvaluator.Default.Instance,
                        this.turnTranslucentEvaluator.Result));
                }
            }
        }

        /// <summary>
        /// Should be set to a BindingAndTriggerValue condition for when the growl should fade out, fires TurnTranslucentChanged RoutedEvent when condition is met
        /// </summary>
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
        /// <summary>
        /// Event for when growl needs to start fading out
        /// </summary>
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
        // define the evaluator for when the growl should stop fading out, defaulting to null
        private BindingEvaluator stopTurningTranslucentEvaluator = null;

        /// <summary>
        /// Property for TurningTranslucentBinding, which should be set to a BindingAndTriggerValue condition for when the growl should stop fading out
        /// </summary>
        public static readonly DependencyProperty StopTurningTranslucentBindingProperty = DependencyProperty.Register(
            "StopTurningTranslucentBinding", typeof(BindingAndTriggerValue), typeof(GrowlBase), new PropertyMetadata(null, new PropertyChangedCallback(StopTurningTranslucentBindingChanged)));

        // handler for when the TurningTranslucentBinding is set; applies the binding
        private static void StopTurningTranslucentBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            // if a binding was previously set, remove it
            if (e.OldValue != null)
            {
                ((GrowlBase)sender).RemoveStopTurningTranslucentBinding();
            }

            // apply the new binding
            ((GrowlBase)sender).ApplyStopTurningTranslucentBinding((BindingAndTriggerValue)e.NewValue);
        }

        // removes the TurningTranslucentBinding if it had an evaluator
        private void RemoveStopTurningTranslucentBinding()
        {
            if (stopTurningTranslucentEvaluator != null)
            {
                stopTurningTranslucentEvaluator.RemoveBinding();
                stopTurningTranslucentEvaluator = null;
            }
        }

        // applies the TurningTranslucentBinding
        private void ApplyStopTurningTranslucentBinding(BindingAndTriggerValue toAdd)
        {
            // if there is a binding to apply, then apply it
            if (toAdd != null)
            {
                // compile the binding comparison for when the data changes
                Func<object, object, bool> runComparison = BuildComparer(toAdd.Value);

                // build the data change action which checks the custom comparison to raise the RoutedEvent
                Action<DependencyPropertyChangedEventArgs> onChanged = (bindingE) =>
                {
                    if (runComparison(toAdd.Value, bindingE.NewValue))
                    {
                        this.RaiseStopTurningTranslucentChanged();
                    }
                };

                // create the BindingEvaluator with the binding comparison and change event handler
                this.stopTurningTranslucentEvaluator = new BindingEvaluator(toAdd.Binding, onChanged);

                // if the bound value is already different from the initial, default value, then immediately fire the changed event to run the comparison
                if (BindingEvaluator.Default.Instance != this.stopTurningTranslucentEvaluator.Result)
                {
                    onChanged(new DependencyPropertyChangedEventArgs(BindingEvaluator.ResultProperty,
                        BindingEvaluator.Default.Instance,
                        this.stopTurningTranslucentEvaluator.Result));
                }
            }
        }

        /// <summary>
        /// Should be set to a BindingAndTriggerValue condition for when the growl should stop fading out, fires TurnOpaqueChanged RoutedEvent when condition is met
        /// </summary>
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
        /// <summary>
        /// Event for when growl needs to stop fading out
        /// </summary>
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
        // define the evaluator for when the growl should immediately become and remain opaque, defaulting to null
        private BindingEvaluator stayOpaqueEvaluator = null;

        /// <summary>
        /// Property for StayOpaqueBinding, which should be set to a BindingAndTriggerValue condition for when the growl should immediately become and remain opaque
        /// </summary>
        public static readonly DependencyProperty StayOpaqueBindingProperty = DependencyProperty.Register(
            "StayOpaqueBinding", typeof(BindingAndTriggerValue), typeof(GrowlBase), new PropertyMetadata(null, new PropertyChangedCallback(StayOpaqueBindingChanged)));

        // handler for when the StayOpaqueBinding is set; applies the binding
        private static void StayOpaqueBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            // if a binding was previously set, remove it
            if (e.OldValue != null)
            {
                ((GrowlBase)sender).RemoveStayOpaqueBinding();
            }

            // apply the new binding
            ((GrowlBase)sender).ApplyStayOpaqueBinding((BindingAndTriggerValue)e.NewValue);
        }

        // removes the StayOpaqueBinding if it had an evaluator
        private void RemoveStayOpaqueBinding()
        {
            if (stayOpaqueEvaluator != null)
            {
                stayOpaqueEvaluator.RemoveBinding();
                stayOpaqueEvaluator = null;
            }
        }

        // applies the StayOpaqueBinding
        private void ApplyStayOpaqueBinding(BindingAndTriggerValue toAdd)
        {
            // if there is a binding to apply, then apply it
            if (toAdd != null)
            {
                // compile the binding comparison for when the data changes
                Func<object, object, bool> runComparison = BuildComparer(toAdd.Value);

                // build the data change action which checks the custom comparison to raise the RoutedEvent
                Action<DependencyPropertyChangedEventArgs> onChanged = (bindingE) =>
                {
                    if (runComparison(toAdd.Value, bindingE.NewValue))
                    {
                        this.RaiseStayOpaqueChanged();
                    }
                };

                // create the BindingEvaluator with the binding comparison and change event handler
                this.stayOpaqueEvaluator = new BindingEvaluator(toAdd.Binding, onChanged);

                // if the bound value is already different from the initial, default value, then immediately fire the changed event to run the comparison
                if (BindingEvaluator.Default.Instance != this.stayOpaqueEvaluator.Result)
                {
                    onChanged(new DependencyPropertyChangedEventArgs(BindingEvaluator.ResultProperty,
                        BindingEvaluator.Default.Instance,
                        this.stayOpaqueEvaluator.Result));
                }
            }
        }

        /// <summary>
        /// Should be set to a BindingAndTriggerValue condition for when the growl should immediately become and remain opaque, fires StayOpaqueChanged RoutedEvent when condition is met
        /// </summary>
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
        /// <summary>
        /// Event for when growl needs to immediately become and remain opaque
        /// </summary>
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