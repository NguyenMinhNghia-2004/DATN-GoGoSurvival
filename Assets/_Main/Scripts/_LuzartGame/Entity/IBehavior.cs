namespace Luzart
{
    public interface IBehavior
    {
        IEntity Owner { get; }
        //void Initialize();
        void Start();
        void Update(float dt);
        void OnDestroy();
    }
}