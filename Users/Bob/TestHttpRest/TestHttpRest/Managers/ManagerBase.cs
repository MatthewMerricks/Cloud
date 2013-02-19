using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public abstract class ManagerBase
    {
        public abstract int Create(Settings.InputParams paramSet, FileInfo fileInfo, string fileName, ref GenericHolder<CLError> ProcessingErrorHolder);
        public abstract int Delete(Settings.InputParams paramSet);
        public abstract int Undelte(Settings.InputParams paramSet);
        public abstract int Rename(Settings.InputParams paramSet);
    }
}
