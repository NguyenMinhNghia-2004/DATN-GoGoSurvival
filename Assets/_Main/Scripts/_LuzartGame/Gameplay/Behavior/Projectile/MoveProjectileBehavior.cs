using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.TextCore.Text;
namespace Luzart
{
    public class MoveProjectileBehavior : BehaviorBase
    {
        public MoveProjectileBehavior(IEntity owner) : base(owner)
        {
        }
        public Vector3 Direction { get; set; }
        public double Speed { get; set; }
        public bool IsMoving => Direction.sqrMagnitude > 0.001f;
        protected override void DoStart()
        {
            base.DoStart();
        }
        protected override void DoUpdate(float dt)
        {
            if (Direction.sqrMagnitude <= 0.001f) return;
            Vector3 currentPos = Owner.Transform.Position.Value;
            Vector3 velocity = Direction * (float)Speed * dt;
            Vector3 newPos = currentPos + velocity;
            Owner.Transform.SetPosition(newPos);
        }
    }
}