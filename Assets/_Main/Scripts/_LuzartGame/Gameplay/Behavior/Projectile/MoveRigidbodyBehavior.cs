using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
namespace Luzart
{
    public class MoveRigidbodyBehavior : BehaviorBase
    {
        public MoveRigidbodyBehavior(IEntity owner) : base(owner)
        {
        }
        private double _currentSpeed;
        private float _gravity;
        private Vector2 _direction;
        private float _maxDistance; 
        private double _currentDistance = 0f;    
        public Action OnHitGround  = null;
        private bool _isBoom = false;
        protected override void DoStart()
        {
            base.DoStart();
            _currentDistance = 0;
            _isBoom = false;
        }
        public void SetDirectAndSpeedProjectile(double speed, Vector2 dir, float gravity)
        {
            _currentSpeed = speed;
            _direction = dir;
            _gravity = gravity;
        }
        public void SetMaxDistance(float maxDistance)
        {
            this._maxDistance = maxDistance;
        }
        protected override void DoUpdate(float dt)
        {
            base.DoUpdate(dt);
            if (_isBoom)
            {
                return;
            }
            if (_currentSpeed <= 0f || _currentDistance > _maxDistance)
            {
                _currentSpeed = 0;
                OnHitGround?.Invoke();
                _isBoom = true;
                return;
            }
            _currentSpeed -= _gravity * dt;
            double deltaDistance = _currentSpeed * dt + 1 / 2 * _gravity * dt * dt;
            _currentDistance += deltaDistance;
            Vector2 newPosition = Owner.Transform.Position.Value + -_direction * (float)deltaDistance;
            Owner.Transform.SetPosition(newPosition);
        }
    }
}
