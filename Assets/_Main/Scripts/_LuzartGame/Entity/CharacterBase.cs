using Luzart;
using System.Collections.Generic;
using System.Linq;
public abstract class CharacterBase : EntityBase, ICharacter
{
    private StatsBehavior _stats;
    public StatsBehavior Stats => _stats;
    public override void Inject(IDomain domain)
    {
        base.Inject(domain);
        _stats = new StatsBehavior(this);
        AddBehavior(_stats);
    }
    public virtual void InitStats(IEnumerable<IStat> configs)
    {
        _stats.Add(configs);
        _stats.RestoreHP();
    }
    public virtual void TakeDamage(double dmg)
    {
        Stats.TakeDamage(dmg);
        if (Stats.IsDead) 
            OnDeath();
    }
}
