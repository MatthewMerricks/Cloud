//
//  CLSptResourceManager.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Resources;

namespace CloudApiPublic.Support
{
    public class CLSptResourceManager
    {
        private static CLSptResourceManager instance = null;
        private static ResourceManager _resMgr = null;
        public ResourceManager ResMgr 
        { 
            get
            {
                return _resMgr;
            }
        }

        /// <summary>
        /// Expose a single instance of this class.
        /// </summary>
        public static CLSptResourceManager Instance
        {
            get
            {
                // If the instance is null then create one and init the Queue
                if (instance == null)
                {
                    instance = new CLSptResourceManager();
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    _resMgr = new ResourceManager(CLSptConstants.kResourcesName, assembly);
                }
                return instance;
            }
        }
        /// <summary>
        /// Private constructor to prevent instance creation
        /// </summary>
        private CLSptResourceManager()
        {
        }
    }
}
