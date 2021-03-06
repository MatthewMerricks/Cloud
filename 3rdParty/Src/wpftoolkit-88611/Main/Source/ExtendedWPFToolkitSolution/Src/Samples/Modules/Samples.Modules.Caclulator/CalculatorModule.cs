﻿/************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2010-2012 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   This program can be provided to you by Xceed Software Inc. under a
   proprietary commercial license agreement for use in non-Open Source
   projects. The commercial version of Extended WPF Toolkit also includes
   priority technical support, commercial updates, and many additional 
   useful WPF controls if you license Xceed Business Suite for WPF.

   Visit http://xceed.com and follow @datagrid on Twitter.

  **********************************************************************/

using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Samples.Infrastructure;
using Samples.Infrastructure.Extensions;
using Samples.Modules.Calculator.NavigationItems;
using Samples.Modules.Calculator.Views;

namespace Samples.Modules.Calculator
{
  public class CalculatorModule : ModuleBase
  {
    public CalculatorModule( IUnityContainer container, IRegionManager regionManager )
      : base( container, regionManager )
    {
    }

    protected override void InitializeModule()
    {
      RegionManager.RegisterViewWithRegion( RegionNames.NavigationRegion, typeof( CalculatorNavItem ) );
      RegionManager.RegisterViewWithRegion( RegionNames.NavigationRegion, typeof( CalculatorUpDownNavItem ) );
    }

    protected override void RegisterViewsAndTypes()
    {
      Container.RegisterNavigationType( typeof( CalculatorView ) );
      Container.RegisterNavigationType( typeof( CalculatorUpDownView ) );
    }
  }
}
