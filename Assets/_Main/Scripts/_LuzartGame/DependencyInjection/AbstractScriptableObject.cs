namespace Luzart
{
    using Luzart;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    public abstract class AbstractScriptableContentSaveable : AbstractScriptableContent, ISaveable
    {
        IEnumerable<SaveItem> ISaveable.Save()
        {
            return DoSave();
        }
        void ISaveable.Load(IEnumerable<SaveItem> saveItems)
        {
            DoLoad(saveItems);
        }
        protected abstract IEnumerable<SaveItem> DoSave();
        protected abstract void DoLoad(IEnumerable<SaveItem> saveItems);
    }
}