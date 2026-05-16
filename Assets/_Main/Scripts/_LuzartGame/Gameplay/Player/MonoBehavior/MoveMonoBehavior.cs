using Unity.VisualScripting;
using UnityEngine;
namespace Luzart
{
    public class MoveMonoBehavior : AbstractMonoBehaviorContent, IEntityBehaviorProvider
    {
        private MoveBehavior _moveBehavior;
        private StatsBehavior _stats;
        private EntityBluePrint _entityBluePrint;
        public void CreateBehavior(IEntity entity)
        {
            _stats = entity.GetBehavior<StatsBehavior>();
            _moveBehavior = new MoveBehavior(entity);
            entity.AddBehavior(_moveBehavior);
            _moveBehavior.OnMove += OnMoved;
        }
        public void InitEntityBluePrint(EntityBluePrint entity)
        {
            _entityBluePrint = entity;
        }
        private void OnMoved(Vector3 direct)
        {
            INumber speedINumber = _stats.Get(StatType.Speed);
            double speed = speedINumber.Value;
            float speedFloat = (float)speed;
            float speedPerFrame = speedFloat * Time.deltaTime;
            Vector3 moveDistance = direct * speedPerFrame;
            _entityBluePrint.transform.position += moveDistance;
        }
        private Vector2 _moveDirection;
        protected override void DoUpdate(float dt)
        {
            base.DoUpdate(dt);
            if (_moveBehavior == null) return;
            ControlByKeyboard();
            _moveDirection = _moveDirection.normalized;
            _moveBehavior.Direction = _moveDirection;
        }
        public override void DoStart()
        {
            base.DoStart();
            Broadcaster.Register<JoystickBroadcastData>(OnControllder);
        }
        public override void DoStop()
        {
            base.DoStop();
            Broadcaster.Unregister<JoystickBroadcastData>(OnControllder);
        }
        private void OnControllder(JoystickBroadcastData data)
        {
            OnControllerJoystick(data.Direction);
        }
        private void OnControllerJoystick(Vector2 direct)
        {
            _moveBehavior.Direction = direct;
        }
        private void ControlByKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                MoveVertical(1);
            }
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveVertical(-1);
            }
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                MoveHorizontal(-1);
            }
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                MoveHorizontal(1);
            }
            if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow))
            {
                OnMouseUpVertical();
            }
            if (Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow))
            {
                OnMouseUpVertical();
            }
            if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow))
            {
                OnMouseUpHorizontal();
            }
            if (Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow))
            {
                OnMouseUpHorizontal();
            }
            void MoveVertical(float value)
            {
                _moveDirection.y = value;
            }
            void MoveHorizontal(float value)
            {
                _moveDirection.x = value;
            }
            void OnMouseUpVertical()
            {
                _moveDirection.y = 0;
            }
            void OnMouseUpHorizontal()
            {
                _moveDirection.x = 0;
            }
        }
    }
}