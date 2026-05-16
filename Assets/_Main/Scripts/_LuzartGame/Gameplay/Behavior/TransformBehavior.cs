using UnityEngine;
using UnityEngine.Pool;
namespace Luzart
{
    public interface ITransform : IBehavior
    {
        IVariable<Vector2> Position { get; }
        IVariable<Vector3> Scale { get; }
        IVariable<Quaternion> Rotation { get; }
        // Helper methods
        void SetPosition(Vector2 newPosition);
        void SetScale(Vector3 newScale);
        void SetRotation(Quaternion newRotation);
        void Translate(Vector2 translation);
    }
    //public static class BehaviorPool<T> where T : IBehavior
    //{
    //    static readonly List<T> _Pool;
    //    public static T Get()
    //    {
    //        _Pool
    //    }
    //    public static void Release(ref T obj)
    //    {
    //        obj = default;
    //    }
    //}
    //public class BehaviorController
    //{
    //    public void Manage(IEntity e)
    //    {
    //        var transformBehavior = BehaviorPool<TransformBehavior>.Get();
    //        e.AddBehavior(transformBehavior);
    //    }
    //    public void Destroy(IEntity e)
    //    {
    //        var transformBehavior = e.GetBehavior<TransformBehavior>();
    //        if (transformBehavior != null)
    //        {
    //            e.RemoveBehavior(transformBehavior);
    //            BehaviorPool<TransformBehavior>.Release(ref transformBehavior);
    //        }
    //    }
    //}
    /// <summary>
    /// TransformBehavior - Quản lý Position, Scale, Rotation của Entity
    /// Tất cả behaviors khác sẽ lấy transform data từ đây
    /// </summary>
    public class TransformBehavior : BehaviorBase, ITransform
    {
        // ITransform implementation - sử dụng IVariable cho reactivity
        public IVariable<Vector2> Position { get; private set; } = new Variable<Vector2>(Vector2.zero);
        public IVariable<Vector3> Scale { get; private set; } = new Variable<Vector3>(Vector3.one);
        public IVariable<Quaternion> Rotation { get; private set; } = new Variable<Quaternion>(Quaternion.identity);
        // Transform matrix cache
        private Matrix4x4 _transformMatrix = Matrix4x4.identity;
        private bool _matrixDirty = true;
        public TransformBehavior(IEntity owner) : base(owner)
        {
        }
        protected override void DoStart()
        {
            // Subscribe to changes để update matrix và events
            System.Action<IValue<Quaternion>> OnAnyPartOfTransformationChanged2 = OnRotationChanged;
            Position.Changed += OnPositionChanged;
            Scale.Changed += OnScaleChanged;
            Rotation.Changed += OnAnyPartOfTransformationChanged2;
            UpdateTransformMatrix();
        }
        protected override void DoUpdate(float dt)
        {
            // Update transform matrix nếu có thay đổi
            if (_matrixDirty)
            {
                UpdateTransformMatrix();
            }
        }
        #region Position Methods
        public void SetPosition(Vector2 newPosition)
        {
            Position.Set(newPosition);
        }
        public void SetPosition(float x, float y, float z)
        {
            SetPosition(new Vector3(x, y, z));
        }
        public void Translate(Vector2 translation)
        {
            SetPosition(Position.Value + translation);
        }
        #endregion
        #region Scale Methods
        public void SetScale(Vector3 newScale)
        {
            Scale.Set(newScale);
        }
        public void SetScale(float uniformScale)
        {
            SetScale(Vector3.one * uniformScale);
        }
        public void SetScale(float x, float y, float z)
        {
            SetScale(new Vector3(x, y, z));
        }
        #endregion
        #region Rotation Methods
        public Vector3 EulerAngles
        {
            get => Rotation.Value.eulerAngles;
            set => SetRotation(Quaternion.Euler(value));
        }
        public void SetRotation(Quaternion newRotation)
        {
            Rotation.Set(newRotation);
        }
        public void SetRotation(Vector3 eulerAngles)
        {
            SetRotation(Quaternion.Euler(eulerAngles));
        }
        public void Rotate(Vector3 eulerAngles)
        {
            SetRotation(Rotation.Value * Quaternion.Euler(eulerAngles));
        }
        #endregion
        #region Matrix Operations
        public Matrix4x4 TransformMatrix
        {
            get
            {
                if (_matrixDirty)
                {
                    UpdateTransformMatrix();
                }
                return _transformMatrix;
            }
        }
        private void UpdateTransformMatrix()
        {
            _transformMatrix = Matrix4x4.TRS(Position.Value, Rotation.Value, Scale.Value);
            _matrixDirty = false;
        }
        /// <summary>
        /// Get transform data cho rendering
        /// </summary>
        public void GetTransformData(out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            position = Position.Value;
            rotation = Rotation.Value;
            scale = Scale.Value;
        }
        #endregion
        #region Utility Methods
        /// <summary>
        /// Get forward direction based on current rotation
        /// </summary>
        public Vector3 Forward => Rotation.Value * Vector3.forward;
        /// <summary>
        /// Get right direction based on current rotation
        /// </summary>
        public Vector3 Right => Rotation.Value * Vector3.right;
        /// <summary>
        /// Get up direction based on current rotation
        /// </summary>
        public Vector3 Up => Rotation.Value * Vector3.up;
        /// <summary>
        /// Transform point from local to world space
        /// </summary>
        public Vector3 TransformPoint(Vector3 localPoint)
        {
            return TransformMatrix.MultiplyPoint3x4(localPoint);
        }
        /// <summary>
        /// Transform direction from local to world space
        /// </summary>
        public Vector3 TransformDirection(Vector3 localDirection)
        {
            return Rotation.Value * localDirection;
        }
        #endregion
        #region Event Handlers
        private void OnPositionChanged(IValue<Vector2> position)
        {
            _matrixDirty = true;
        }
        private void OnScaleChanged(IValue<Vector3> scale)
        {
            _matrixDirty = true;
        }
        private void OnRotationChanged(IValue<Quaternion> rotation)
        {
            _matrixDirty = true;
        }
        #endregion
        protected override void DoDestroy()
        {
            System.Action<IValue<Quaternion>> OnAnyPartOfTransformationChanged2 = OnRotationChanged;
            Position.Changed -= OnPositionChanged;
            Scale.Changed -= OnScaleChanged;
            Rotation.Changed -= OnAnyPartOfTransformationChanged2;
        }
    }
}