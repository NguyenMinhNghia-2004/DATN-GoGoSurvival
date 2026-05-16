using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class MoveProjectileBoomerangBehavior : BehaviorBase
    {
        private BoomerangProjectileConfig _config;
        private ITransform Transform => Owner.Transform;
        private IEntity _projectileOwner; // The actual owner who threw the boomerang
        private Vector3 _startPosition;
        private float _distanceTraveled;
        private bool _isReturning = false;
        private float _currentSpeed;
        private float _visualRotation = 0f;
        private float _currentTime = 0f;
        // Default max outward distance
        private Vector2 _direction;
        private double _maxSpeed;
        public MoveProjectileBoomerangBehavior(IEntity owner, BoomerangProjectileConfig projectileConfig, IEntity projectileOwner) : base(owner)
        {
            this._config = projectileConfig;
            this._projectileOwner = projectileOwner;
        }
        protected override void DoStart()
        {
            base.DoStart();
            // Initialize boomerang parameters
            _startPosition = Owner.Transform.Position.Value;
            _distanceTraveled = 0f;
            _isReturning = false;
        }
        protected override void DoUpdate(float dt)
        {
            if (!_isReturning)
            {
                UpdateOutwardPhase(dt);
            }
            else
            {
                UpdateReturnPhase(dt);
            }
            _visualRotation += _config.RotationSpeed * dt;
            var visualQuaternion = Quaternion.Euler(new Vector3(0f, 0f, _visualRotation));
            Transform.SetRotation(visualQuaternion);
        }
        private void UpdateOutwardPhase(float dt)
        {
            _currentTime += dt;
            _currentSpeed = (float)_maxSpeed - _config.Acceration * _currentTime;
            _currentSpeed = Mathf.Clamp(_currentSpeed, -0.1f, (float)_maxSpeed);
            float deltaDistance = _currentSpeed * dt + 1 / 2 * (-_config.Acceration) * dt * dt; 
            if (_currentSpeed <= 0)
            {
                _isReturning = true;
                _currentTime = 0f;
                return;
            }
            Vector2 newPosition = Transform.Position.Value + deltaDistance*_direction;
            Transform.SetPosition(newPosition);
        }
        private void UpdateReturnPhase(float dt)
        {
            _currentTime += dt;
            Vector3 ownerPosition = GetOwnerCurrentPosition();
            Vector2 currentPosition = Transform.Position.Value;
            //Vector3 direction = (ownerPosition - currentPosition).normalized;
            _currentSpeed = _config.AccerationReturn * _currentTime;
            float deltaDistance = _currentSpeed * dt + 1 / 2 * (_config.AccerationReturn) * dt * dt;
            Vector2 newPosition = currentPosition + -_direction* deltaDistance;
            Owner.Transform.SetPosition(newPosition);
            if (Vector2.Distance(Transform.Position.Value, ownerPosition) < 1.5f)
            {
                Owner.OnDeath();
            }
        }
        private Vector3 GetOwnerCurrentPosition()
        {
            if (_projectileOwner != null && !_projectileOwner.IsDead)
            {
                return _projectileOwner.Transform.Position.Value;
            }
            return _startPosition;
        }
        public void SetDirectAndSpeedProjectile(double speed, Vector3 direction)
        {
            _direction = direction.normalized;
            _maxSpeed = speed;
        }
    }
}
