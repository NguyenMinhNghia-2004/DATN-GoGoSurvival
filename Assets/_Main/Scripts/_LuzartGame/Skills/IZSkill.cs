using System;
using System.Collections.Generic;
namespace Luzart
{
    public interface IZSkill : IDisposable, IBehavior
    {
        IEntity Entity { get; }
        ZSkillConfig Config { get; }
        INumberWithSet LevelIndex { get; }
        IReadOnlyList<IZSkillBehavior> Behaviors { get; }
        void UpgradeSkill();
        bool IsUpgradeSkill();
    }
}