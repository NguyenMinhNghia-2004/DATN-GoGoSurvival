namespace Luzart
{
    using System.Collections.Generic;
    public interface ISaveable
    {
        IEnumerable<SaveItem> Save();
        void Load(IEnumerable<SaveItem> saveItems);
    }
    public readonly struct SaveItem
    {
        public readonly string key;
        public readonly ValueSaveType valueType;
        public readonly bool boolValue;
        public readonly int intValue;
        public readonly float floatValue;
        public readonly double doubleValue;
        public readonly string stringValue;
        public SaveItem(string key, bool boolValue) : this()
        {
            this.key = key;
            this.boolValue = boolValue;
            valueType = ValueSaveType.Bool;
        }
        public SaveItem(string key, int intValue) : this()
        {
            this.key = key;
            this.intValue = intValue;
            valueType = ValueSaveType.Int;
        }
        public SaveItem(string key, float floatValue) : this()
        {
            this.key = key;
            this.floatValue = floatValue;
            valueType = ValueSaveType.Float;
        }
        public SaveItem(string key, string stringValue) : this()
        {
            this.key = key;
            this.stringValue = stringValue;
            valueType = ValueSaveType.String;
        }
        public SaveItem(string key, double doubleValue) : this()
        {
            this.key = key;
            this.doubleValue = doubleValue;
            valueType = ValueSaveType.Double;
        }
    }
    public enum ValueSaveType
    {
        Int = 0,
        Float = 1,
        String = 2,
        Bool = 3,
        Double = 4
    }
}