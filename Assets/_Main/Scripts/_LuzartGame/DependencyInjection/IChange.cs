using System;
using System.Collections.Generic;
using System.Diagnostics;
//public interface IValue
//{
//    double Value { get; }
//    event Action<IValue> OnChanged;
//}
//public interface IBool
//{
//    bool Value { get; }
//    event Action<IBool> OnChanged;
//}
//public interface IString
//{
//    string Value { get; }
//    event Action<IString> OnChanged;
//}
//public class VariableValue : IValue
//{
//    public static implicit operator double(VariableValue v) => v.Value;
//    public VariableValue(double value)
//    {
//        this._value = value;
//    }
//    private double _value;
//    public double Value
//    {
//        get => _value;
//        set
//        {
//            if (_value != value)
//            {
//                _value = value;
//                OnChanged?.Invoke(this);
//            }
//        }
//    }
//    public event Action<IValue> OnChanged;
//}
//public class VariableBool : IBool
//{
//    public static implicit operator bool(VariableBool v) => v.Value;
//    public VariableBool(bool value)
//    {
//        this._value = value;
//    }
//    private bool _value;
//    public bool Value
//    {
//        get => _value;
//        set
//        {
//            if (_value != value)
//            {
//                _value = value;
//                OnChanged?.Invoke(this);
//            }
//        }
//    }
//    public event Action<IBool> OnChanged;
//}
//public class VariableString : IString
//{
//    public static implicit operator string(VariableString v) => v.Value;
//    public VariableString(string value)
//    {
//        this._value = value;
//    }
//    private string _value;
//    public string Value
//    {
//        get => _value;
//        set
//        {
//            if (_value != value)
//            {
//                _value = value;
//                OnChanged?.Invoke(this);
//            }
//        }
//    }
//    public event Action<IString> OnChanged;
//}
//public class SumVariable : IVariable<float>
//{
//    List<IVariable<float>> _elements;
//    float _sum;
//    float IVariable<float>.Value => _sum;
//    event Action<IVariable<float>> _Changed;
//    event Action<IVariable<float>> IVariable<float>.Changed
//    {
//        add => _Changed += value;
//        remove => _Changed -= value;
//    }
//    public SumVariable(List<IVariable<float>> elements)
//    {
//        _elements = elements;
//        foreach (var e in _elements)
//        {
//            e.Changed += E_Changed;
//        }
//        Recalculate();
//    }
//    private void E_Changed(IVariable<float> obj)
//    {
//        Recalculate();
//    }
//    void Recalculate()
//    {
//        var s = 0f;
//        foreach (var e in _elements)
//        {
//            s += e.Value;
//        }
//        if (s != _sum)
//        {
//            _sum = s;
//            _Changed?.Invoke(this);
//        }
//    }
//}
public interface IValue<T>
{
    T Value { get; }
    event Action<IValue<T>> Changed;
}
public class Value<T> : IValue<T>
{
    public Value(T intial = default)
    {
        _value = intial;
    }
    private T _value;
    T IValue<T>.Value => _value;
    public event Action<IValue<T>> Changed;
}
public interface IVariable<T> : IValue<T>
{
    void Set(T value);
}
public class Variable<T> : IVariable<T>
{
    private T _value;
    private readonly IEqualityComparer<T> _comparer;
    public event Action<IValue<T>> Changed;
    public T Value => _value;
    T IValue<T>.Value => _value;
    public Variable(T initial = default, IEqualityComparer<T> comparer = null, Action<IValue<T>> OnChange = null)
    {
        _value = initial;
        _comparer = comparer ?? EqualityComparer<T>.Default;
        this.Changed = OnChange;
    }
    void IVariable<T>.Set(T value)
    {
        if (_comparer != null && _comparer.Equals(_value, value)) return;
        _value = value;
        this.Changed?.Invoke(this);
    }
    public static implicit operator T(Variable<T> v) => v._value;
    public static implicit operator Variable<T>(T v) => new Variable<T>(v);
}
#region Extend
public struct VariableStruct<T> : IValue<T>
{
    private T _value;
    private readonly IEqualityComparer<T> _comparer;
    public VariableStruct(T initial = default, IEqualityComparer<T> comparer = null, Action<IValue<T>> OnChange = null)
    {
        _value = initial;
        _comparer = comparer ?? EqualityComparer<T>.Default;
        this.Changed = OnChange;
    }
    public T Value
    {
        get => _value;
        set
        {
            if (!_comparer.Equals(_value, value))
            {
                var old = _value;
                _value = value;
                Changed?.Invoke(this);
            }
        }
    }
    public event Action<IValue<T>> Changed;
    public static implicit operator T(VariableStruct<T> v) => v._value;
    public static implicit operator VariableStruct<T>(T v) => new VariableStruct<T>(v);
}
#region Comparers
public sealed class DoubleComparer : IEqualityComparer<double>
{
    private readonly double _eps;
    public DoubleComparer()
    {
        _eps = 1e-6;
    }
    public DoubleComparer(double eps = 1e-6) => _eps = eps;
    public bool Equals(double x, double y)
        => Math.Abs(x - y) <= _eps || (double.IsNaN(x) && double.IsNaN(y));
    public int GetHashCode(double obj) => 0;
}
public sealed class FloatComparer : IEqualityComparer<float>
{
    private readonly float _eps;
    public FloatComparer()
    {
        _eps = 1e-6f;
    }
    public FloatComparer(float eps = 1e-6f) => _eps = eps;
    public bool Equals(float x, float y)
        => Math.Abs(x - y) <= _eps || (float.IsNaN(x) && float.IsNaN(y));
    public int GetHashCode(float obj) => 0;
}
public sealed class Vector3Comparer : IEqualityComparer<UnityEngine.Vector3>
{
    private readonly double _eps;
    public Vector3Comparer()
    {
        _eps = 1e-6;
    }
    public Vector3Comparer(double eps = 1e-6) => _eps = eps;
    public bool Equals(UnityEngine.Vector3 x, UnityEngine.Vector3 y)
        => (x - y).sqrMagnitude <= _eps * _eps;
    public int GetHashCode(UnityEngine.Vector3 obj) => 0;
}
#endregion
#endregion
