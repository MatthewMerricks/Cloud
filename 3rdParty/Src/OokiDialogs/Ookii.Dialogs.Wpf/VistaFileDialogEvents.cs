// Copyright (c) Sven Groot (Ookii.org) 2006
// See license.txt for details
using System;
using System.Collections.Generic;
using System.Text;

namespace Ookii.Dialogs.WpfMinusTaskDialog
{
    class VistaFileDialogEvents : Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialogEvents, Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialogControlEvents
    {
        const uint S_OK = 0;
        const uint S_FALSE = 1;
        const uint E_NOTIMPL = 0x80004001;

        private VistaFileDialog _dialog;

        public VistaFileDialogEvents(VistaFileDialog dialog)
        {
            if( dialog == null )
                throw new ArgumentNullException("dialog");

            _dialog = dialog;
        }

        #region IFileDialogEvents Members

        public Ookii.Dialogs.WpfMinusTaskDialog.Interop.HRESULT OnFileOk(Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialog pfd)
        {
            if( _dialog.DoFileOk(pfd) )
                return Ookii.Dialogs.WpfMinusTaskDialog.Interop.HRESULT.S_OK;
            else
                return Ookii.Dialogs.WpfMinusTaskDialog.Interop.HRESULT.S_FALSE;
        }

        public Ookii.Dialogs.WpfMinusTaskDialog.Interop.HRESULT OnFolderChanging(Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialog pfd, Ookii.Dialogs.WpfMinusTaskDialog.Interop.IShellItem psiFolder)
        {
            return Ookii.Dialogs.WpfMinusTaskDialog.Interop.HRESULT.S_OK;
        }

        public void OnFolderChange(Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialog pfd)
        {
        }

        public void OnSelectionChange(Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialog pfd)
        {
        }

        public void OnShareViolation(Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialog pfd, Ookii.Dialogs.WpfMinusTaskDialog.Interop.IShellItem psi, out NativeMethods.FDE_SHAREVIOLATION_RESPONSE pResponse)
        {
            pResponse = NativeMethods.FDE_SHAREVIOLATION_RESPONSE.FDESVR_DEFAULT;
        }

        public void OnTypeChange(Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialog pfd)
        {
        }

        public void OnOverwrite(Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialog pfd, Ookii.Dialogs.WpfMinusTaskDialog.Interop.IShellItem psi, out NativeMethods.FDE_OVERWRITE_RESPONSE pResponse)
        {
            pResponse = NativeMethods.FDE_OVERWRITE_RESPONSE.FDEOR_DEFAULT;
        }

        #endregion

        #region IFileDialogControlEvents Members

        public void OnItemSelected(Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialogCustomize pfdc, int dwIDCtl, int dwIDItem)
        {
        }

        public void OnButtonClicked(Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialogCustomize pfdc, int dwIDCtl)
        {
        }

        public void OnCheckButtonToggled(Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialogCustomize pfdc, int dwIDCtl, bool bChecked)
        {
        }

        public void OnControlActivating(Ookii.Dialogs.WpfMinusTaskDialog.Interop.IFileDialogCustomize pfdc, int dwIDCtl)
        {
        }

        #endregion


    }
}
