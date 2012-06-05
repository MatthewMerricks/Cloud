using System;
using System.Windows;
using System.Windows.Controls;

namespace Dialog.Abstractions.Wpf.Intefaces
{
    public interface IModalWindow
    {
        bool? DialogResult { get; set; }
        event EventHandler Closed;
        void Show();
        //void Show(Grid gridContainer); //now implemented via extension method in Xceed.Wpf.Toolkit.ChildWindowExtensions (Dialog.Implementors.Wpf\Extensions\ChildWindowExtensions.cs)
        object DataContext { get; set; }
        void Close();
    }
}