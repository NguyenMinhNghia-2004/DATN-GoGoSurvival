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
        private bool _isParabola = false;
        private Vector2 _startPos;
        private Vector2 _targetPos;
        private float _parabolaTime;
        private float _elapsedTime;
        private float _vx;
        private float _vy;
        protected override void DoStart()
        {
            base.DoStart();
            _currentDistance = 0;
            _isBoom = false;
            _isParabola = false;
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
        public void SetParabola(Vector2 startPos, Vector2 targetPos, float vx, float vy, float time, float gravity)
        {
            _isParabola = true;
            _startPos = startPos;
            _targetPos = targetPos;
            _vx = vx;
            _vy = vy;
            _parabolaTime = time;
            _gravity = gravity;
            _elapsedTime = 0f;
            Owner.Transform.SetPosition(startPos);
        }
        protected override void DoUpdate(float dt)
        {
            base.DoUpdate(dt);
            if (_isBoom)
            {
                return;
            }
            if (_isParabola)
            {
                _elapsedTime += dt;
                if (_elapsedTime >= _parabolaTime)
                {
                    Owner.Transform.SetPosition(_targetPos);
                    OnHitGround?.Invoke();
                    _isBoom = true;
                    return;
                }
                float t = _elapsedTime;
                float currentX = _startPos.x + _vx * t;
                float currentY = _startPos.y + _vy * t - 0.5f * _gravity * t * t;
                Owner.Transform.SetPosition(new Vector2(currentX, currentY));

                Vector2 vel = new Vector2(_vx, _vy - _gravity * t);
                float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
                Owner.Transform.SetRotation(Quaternion.Euler(0f, 0f, angle - 90f));
                
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
            Vector2 newPosition = Owner.Transform.Position.Value + _direction * (float)deltaDistance;
            Owner.Transform.SetPosition(newPosition);
        }
    }
}
