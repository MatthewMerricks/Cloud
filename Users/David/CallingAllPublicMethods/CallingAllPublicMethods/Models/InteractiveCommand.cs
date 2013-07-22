using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace CallingAllPublicMethods.Models
{
    public sealed class InteractiveCommand : TriggerAction<DependencyObject>
    {
        protected override void Invoke(object parameter)
        {
            if (base.AssociatedObject != null)
            {
                ICommand command = this.ResolveCommand();
                if (command != null
                    && command.CanExecute(parameter))
                {
                    command.Execute(parameter);
                }
            }
        }

        private ICommand ResolveCommand()
        {
            ICommand command = null;
            if ((command = this.Command) == null
                && base.AssociatedObject != null)
            {
                foreach (PropertyInfo info in base.AssociatedObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (typeof(ICommand).IsAssignableFrom(info.PropertyType) && string.Equals(info.Name, this._commandName, StringComparison.Ordinal))
                    {
                        command = (ICommand)info.GetValue(base.AssociatedObject, index: null);
                    }
                }
            }
            return command;
        }

        private string _commandName = null;
        public string CommandName
        {
            get
            {
                base.ReadPreamble();
                return this._commandName;
            }
            set
            {
                if (this._commandName != value)
                {
                    base.WritePreamble();
                    this._commandName = value;
                    base.WritePostscript();
                }
            }
        }

        #region Command

        public ICommand Command
        {
            get
            {
                return (ICommand)GetValue(CommandProperty);
            }
            set
            {
                SetValue(CommandProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                ((MemberExpression)((Expression<Func<InteractiveCommand, ICommand>>)(parent => parent.Command)).Body).Member.Name,
                typeof(ICommand),
                typeof(InteractiveCommand),
                new UIPropertyMetadata(defaultValue: null));

        #endregion
    }
}