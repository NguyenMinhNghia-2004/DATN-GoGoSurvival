using System;
using UnityEngine;
namespace Luzart
{
    public class MoveBehavior : BehaviorBase
    {
        private StatsBehavior stats;
        private ICharacter character;
        public Action<Vector3> OnMove;
        public MoveBehavior(IEntity owner) : base(owner)
        {
        }
        public Vector3 Direction { get; set; }
        public bool IsMoving => Direction.sqrMagnitude > 0.001f;
        protected override void DoStart()
        {
            base.DoStart();
            character = Owner as ICharacter;
            if (stats == null)
                stats = character?.Stats;
        }
        protected override void DoUpdate(float dt)
        {
            if (Direction.sqrMagnitude <= 0.001f) return;
            Vector3 currentPos = Owner.Transform.Position.Value;
            Vector3 velocity = Direction * (float)stats.Get(StatType.Speed).Value * dt;
            Vector3 newPos = currentPos + velocity;
            OnMove?.Invoke(Direction);
            Owner.Transform.SetPosition(newPos);
        }
    }
}
