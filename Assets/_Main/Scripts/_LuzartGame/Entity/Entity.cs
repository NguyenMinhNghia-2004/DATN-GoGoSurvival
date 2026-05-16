using Luzart;
// IEntity: đơn vị runtime tối thiểu
public interface IEntity : IContent
{
    ITransform Transform { get; }
    bool IsDead { get; }
    //void OnSpawn(UnityEngine.Vector3 pos);
    void OnDeath();
    void OnUpdate(float dt);
    // Behavior management
    System.Collections.Generic.IReadOnlyList<IBehavior> Behaviors { get; }
    T GetBehavior<T>() where T : IBehavior;
    void AddBehavior<T>(T behavior) where T : IBehavior;
}
// ICharacter: nhân vật → có Stats & TakeDamage
public interface ICharacter : IEntity
{
    StatsBehavior Stats { get; }
}
