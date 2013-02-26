using CloudApiPublic.Model;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public abstract class ManagerBase
    {
        public abstract int Create(Settings.InputParams paramSet,SmokeTask smokeTask, FileInfo fileInfo, string fileName, ref StringBuilder ReportBuilder,  ref GenericHolder<CLError> ProcessingErrorHolder);
        public abstract int Delete(Settings.InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder);
        public abstract int Undelete(Settings.InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder);
        public abstract int Rename(Settings.InputParams paramSet, SmokeTask smokeTask, string directoryRelativetoRoot, string oldName, string newName, ref StringBuilder reportBuilder, ref GenericHolder<CLError> ProcessingErrorHolder);
    }
}
