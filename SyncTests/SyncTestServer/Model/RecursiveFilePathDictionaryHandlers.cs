using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncTestServer.Model
{
    public sealed class RecursiveFilePathDictionaryHandlers<TDictionaryValue, TStateType>
    {
        private readonly TStateType UserState;

        public event EventHandler<RecursiveDeleteArgs<TDictionaryValue, TStateType>> RecursedDelete
        {
            add
            {
                lock (RecursedDeleteLocker)
                {
                    _recursedDelete += value;
                }
            }
            remove
            {
                lock (RecursedDeleteLocker)
                {
                    _recursedDelete -= value;
                }
            }
        }
        private event EventHandler<RecursiveDeleteArgs<TDictionaryValue, TStateType>> _recursedDelete;
        private readonly object RecursedDeleteLocker = new object();
        private void FowardDeleteEvent(RecursiveDeleteArgs<TDictionaryValue, TStateType> args)
        {
            lock (RecursedDeleteLocker)
            {
                if (_recursedDelete != null)
                {
                    _recursedDelete(this, args);
                }
            }
        }

        public event EventHandler<RecursiveRenameArgs<TDictionaryValue, TStateType>> RecursedRename
        {
            add
            {
                lock (RecursedRenameLocker)
                {
                    _recursedRename += value;
                }
            }
            remove
            {
                lock (RecursedRenameLocker)
                {
                    _recursedRename -= value;
                }
            }
        }
        private event EventHandler<RecursiveRenameArgs<TDictionaryValue, TStateType>> _recursedRename;
        private readonly object RecursedRenameLocker = new object();
        private void FowardRenameEvent(RecursiveRenameArgs<TDictionaryValue, TStateType> args)
        {
            lock (RecursedRenameLocker)
            {
                if (_recursedRename != null)
                {
                    _recursedRename(this, args);
                }
            }
        }

        public RecursiveFilePathDictionaryHandlers(TStateType state)
        {
            this.UserState = state;
        }

        public void RecursiveDeleteCallback(FilePath removedPath, TDictionaryValue deletedValue, FilePath changeRoot)
        {
            FowardDeleteEvent(new RecursiveDeleteArgs<TDictionaryValue, TStateType>(this.UserState, removedPath, deletedValue, changeRoot));
        }

        public void RecursiveRenameCallback(FilePath removedFromPath, FilePath addedNewPath, TDictionaryValue movedValue, FilePath changedOldRoot, FilePath changedNewRoot)
        {
            FowardRenameEvent(new RecursiveRenameArgs<TDictionaryValue, TStateType>(this.UserState, removedFromPath, addedNewPath, movedValue, changedOldRoot, changedNewRoot));
        }
    }

    public sealed class RecursiveDeleteArgs<TDictionaryValue, TStateType> : EventArgs
    {
        public TStateType UserState
        {
            get
            {
                return _userState;
            }
        }
        private readonly TStateType _userState;

        public FilePath DeletedPath
        {
            get
            {
                return _deletedPath;
            }
        }
        private readonly FilePath _deletedPath;

        public TDictionaryValue DeletedValue
        {
            get
            {
                return _deletedValue;
            }
        }
        private TDictionaryValue _deletedValue;

        public FilePath ChangeRoot
        {
            get
            {
                return _changeRoot;
            }
        }
        private readonly FilePath _changeRoot;

        public RecursiveDeleteArgs(TStateType state, FilePath DeletedPath, TDictionaryValue DeletedValue, FilePath ChangeRoot)
        {
            this._userState = state;
            this._deletedPath = DeletedPath;
            this._deletedValue = DeletedValue;
            this._changeRoot = ChangeRoot;
        }
    }

    public sealed class RecursiveRenameArgs<TDictionaryValue, TStateType> : EventArgs
    {
        public TStateType UserState
        {
            get
            {
                return _userState;
            }
        }
        private readonly TStateType _userState;

        public FilePath DeletedFromPath
        {
            get
            {
                return _deletedFromPath;
            }
        }
        private readonly FilePath _deletedFromPath;

        public FilePath AddedToPath
        {
            get
            {
                return _addedToPath;
            }
        }
        private readonly FilePath _addedToPath;

        public TDictionaryValue MovedValue
        {
            get
            {
                return _movedValue;
            }
        }
        private TDictionaryValue _movedValue;

        public FilePath ChangedOldRoot
        {
            get
            {
                return _changedOldRoot;
            }
        }
        private readonly FilePath _changedOldRoot;

        public FilePath ChangedNewRoot
        {
            get
            {
                return _changedNewRoot;
            }
        }
        private readonly FilePath _changedNewRoot;

        public RecursiveRenameArgs(TStateType state, FilePath DeletedFromPath, FilePath AddedToPath, TDictionaryValue MovedValue, FilePath ChangedOldRoot, FilePath ChangedNewRoot)
        {
            this._userState = state;
            this._deletedFromPath = DeletedFromPath;
            this._addedToPath = AddedToPath;
            this._movedValue = MovedValue;
            this._changedOldRoot = ChangedOldRoot;
            this._changedNewRoot = ChangedNewRoot;
        }
    }
}