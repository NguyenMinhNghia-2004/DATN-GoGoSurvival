using UnityEngine;
namespace Luzart
{
    public class XPDropEntity : DropEntity
    {
        public XPDropEntity(DropConfig config, IEntity owner) : base(config, owner)
        {
        }
        protected override bool CanPickupImpl(IEntity picker)
        {
            if(picker is PlayerCharacter)
            {
                return true;
            }
            return false;
        }
        protected override void OnPickupImpl(IEntity picker)
        {
            var stats = picker.GetBehavior<StatsBehavior>();
            if(stats != null)
            {
                stats.AddXP(((XPDropConfig)_dropConfig).XPAmount);
            }
        }
    }
}