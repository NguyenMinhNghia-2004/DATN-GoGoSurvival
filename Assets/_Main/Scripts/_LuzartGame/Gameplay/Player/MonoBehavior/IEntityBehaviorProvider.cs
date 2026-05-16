namespace Luzart
{
    public interface IEntityBehaviorProvider
    {
        void CreateBehavior(IEntity entity);
        void InitEntityBluePrint(EntityBluePrint entity);
    }
}