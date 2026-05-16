using Luzart;
using UnityEngine;
namespace Luzart
{
    public interface IStatDefinition
    {
        StatType StatType { get; }
        IString DisplayName { get; }
        Sprite Icon { get; }
    }
}
