using Luzart;
using System;
using UnityEngine;
public sealed class StatKey : ScriptableObject, IEquatable<string>
{
    [SerializeField] string key;
    public string Id => key;
    public bool Equals(string other)
    {
        return Id.Equals(other, StringComparison.Ordinal);
    }
    public static implicit operator string(StatKey key) => key.Id;
}