using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Luzart
{
    public class MoveTargetBehavior : BehaviorBase
    {
        private IEntity _target;
        private float _speed;
        private bool _isCantRotation = false;
        private bool _isFollowNow = false;
        public MoveTargetBehavior(IEntity owner) : base(owner)
        {
        }
        public MoveTargetBehavior(IEntity owner, bool isCantRotation = false, bool isFollowNow = false) : base(owner)
        {
            this._isCantRotation = isCantRotation;
            this._isFollowNow = isFollowNow;
        }
        public void SetTarget(IEntity target)
        {
            this._target = target;
        }
        public void SetSpeed(float speed)
        {
            this._speed = speed;
        }
        protected override void DoUpdate(float dt)
        {
            base.DoUpdate(dt);
            OnMoveToTarget(dt);
        }
        private Vector2 Direction;
        private void OnMoveToTarget(float dt)
        {
            if (_target == null)
            {
                return;
            }
            Direction = Vector2.zero;
            Direction = _target.Transform.Position.Value - Owner.Transform.Position.Value;
            float distance = Direction.magnitude;
            if (distance < 0.001f)
            {
                return;
            }
            Direction.Normalize();
            if (!_isCantRotation)
            {
                OnRotation();
            }
            if (_isFollowNow)
            {
                OnFollowNow(dt);
            }
            else
            {
                OnMove(dt);
            }
        }
        private void OnRotation()
        {
            float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
            Quaternion quaternion = Quaternion.identity;
            quaternion = Quaternion.Euler(new Vector3(0f, 0f, angle - 90f));
            Owner.Transform.SetRotation(quaternion);
        }
        private void OnMove(float dt)
        {
            Vector2 newPos = Owner.Transform.Position.Value + _speed * Direction * dt;
            Owner.Transform.SetPosition(newPos);
        }
        private void OnFollowNow(float dt)
        {
            Owner.Transform.SetPosition(_target.Transform.Position.Value);
        }
    }
}
