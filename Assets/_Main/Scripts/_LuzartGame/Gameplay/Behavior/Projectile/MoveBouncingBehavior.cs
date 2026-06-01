using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Movement behavior that reflects on camera edges. Reads Camera.main viewport every
    /// frame so it tracks a moving camera. Direction is reflected component-wise — only
    /// the offending axis flips, keeping the orthogonal component intact (so a bullet
    /// hitting the right wall continues moving up/down at the same rate).
    /// </summary>
    public class MoveBouncingBehavior : BehaviorBase
    {
        private readonly BouncingProjectileConfig _config;
        private int _bouncesUsed;
        private Camera _cam;

        public Vector3 Direction { get; set; }
        public double Speed { get; set; }

        public MoveBouncingBehavior(IEntity owner, BouncingProjectileConfig cfg) : base(owner)
        {
            _config = cfg;
        }

        protected override void DoStart()
        {
            base.DoStart();
            _cam = Camera.main;
        }

        protected override void DoUpdate(float dt)
        {
            if (Direction.sqrMagnitude <= 0.001f || _cam == null) return;
            Vector3 cur = Owner.Transform.Position.Value;
            Vector3 next = cur + Direction * (float)Speed * dt;

            // Camera bounds → world bounds via viewport corners.
            Vector3 min = _cam.ViewportToWorldPoint(new Vector3(0f, 0f, _cam.nearClipPlane));
            Vector3 max = _cam.ViewportToWorldPoint(new Vector3(1f, 1f, _cam.nearClipPlane));

            bool bounced = false;
            if (next.x > max.x) { Direction = new Vector3(-Mathf.Abs(Direction.x), Direction.y, Direction.z); bounced = true; }
            if (next.x < min.x) { Direction = new Vector3(Mathf.Abs(Direction.x), Direction.y, Direction.z); bounced = true; }
            if (next.y > max.y) { Direction = new Vector3(Direction.x, -Mathf.Abs(Direction.y), Direction.z); bounced = true; }
            if (next.y < min.y) { Direction = new Vector3(Direction.x, Mathf.Abs(Direction.y), Direction.z); bounced = true; }

            if (bounced)
            {
                _bouncesUsed++;
                if (_config.MaxBounces >= 0 && _bouncesUsed > _config.MaxBounces)
                {
                    Owner.OnDeath();
                    return;
                }
                // Recompute next position with reflected direction this frame so it doesn't escape bounds.
                next = cur + Direction * (float)Speed * dt;
            }
            Owner.Transform.SetPosition(next);
        }

        /// <summary>Called by BouncingProjectile.OnCollision when pierce=false. Reflects
        /// direction back along the line from enemy to projectile (simple bounce-off).</summary>
        public void ReflectFromEnemy(IEntity enemy)
        {
            if (enemy?.Transform == null) return;
            Vector2 away = Owner.Transform.Position.Value - enemy.Transform.Position.Value;
            if (away.sqrMagnitude < 0.001f) away = -(Vector2)Direction; // overlap fallback
            Direction = away.normalized;
        }
    }
}
