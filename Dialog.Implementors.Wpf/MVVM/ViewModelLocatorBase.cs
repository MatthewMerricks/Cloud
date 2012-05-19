using System.ComponentModel;
using Dialog.Abstractions.Wpf.ServiceLocation;
using GalaSoft.MvvmLight;

namespace Dialog.Implementors.Wpf.MVVM
{
  public abstract class ViewModelLocatorBase<TViewModel> : NotifyPropertyChangedEnabledBase where TViewModel : class 
  {
    private static bool? isInDesignMode;
    private TViewModel runtimeViewModel;
    private TViewModel designtimeViewModel;

    /// <summary>
    /// Gets a value indicating whether the control is in design mode
    /// (running in Blend or Visual Studio).
    /// </summary>
    public static bool IsInDesignMode
    {
      get
      {
        if (!isInDesignMode.HasValue)
        {
            //isInDesignMode = DesignerProperties.IsInDesignTool;
            isInDesignMode = ViewModelBase.IsInDesignModeStatic;
       }
                
        return isInDesignMode.Value;
      }
    }

    protected TViewModel RuntimeViewModel
    {
      get
      {
        if (this.runtimeViewModel == null)
        {
          this.RuntimeViewModel = SimpleServiceLocator.Instance.Get<TViewModel>();
        }
        return runtimeViewModel;
      }

      set 
      { 
        runtimeViewModel = value;
        this.OnPropertyChanged("ViewModel");
      }
    }

    public TViewModel ViewModel
    {
      get
      {
        return IsInDesignMode ? this.DesigntimeViewModel : this.RuntimeViewModel;
      }
    }

    public TViewModel DesigntimeViewModel
    {
      get
      {
        return designtimeViewModel;
      }
      
      set 
      { 
        designtimeViewModel = value;
        this.OnPropertyChanged("ViewModel");
      }
    }
  }
}