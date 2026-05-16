using Luzart;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Mapping : IContent
{
    Dictionary<IEntity, ICollider> EntityCollider = new Dictionary<IEntity, ICollider>();
    Dictionary<ICollider, IEntity> EntityColliderFlip = new Dictionary<ICollider, IEntity>();
    string IContent.Id => "Mapping";
    private IDomain _domain;
    IDomain IContent.MyDomain => _domain;
    public void AddCollider(IEntity entity, ICollider collider)
    {
        EntityCollider[entity] = collider;
        EntityColliderFlip[collider] = entity;
    }
    public IEntity FindEntityWithCollider(ICollider collider)
    {
        //foreach(var pair in EntityCollider)
        //{
        //    if(pair.Value == collider)
        //    {
        //        return pair.Key;
        //    }
        //}
        if(EntityColliderFlip.TryGetValue(collider, out var entity))
        {
            return entity;
        }
        return null;
    }
    public ICollider FindColliderWithEntity(IEntity entity)
    {
        if (EntityCollider.TryGetValue(entity, out var collider))
        {
            return collider;
        }
        return null;
    }
    public void RemoveEntity(IEntity entity)
    {
        var key = EntityCollider[entity];
        EntityCollider.Remove(entity);
        EntityColliderFlip.Remove(key);
    }
    public void RemoveCollider(ICollider collider)
    {
        var entityToRemove = FindEntityWithCollider(collider);
        if (entityToRemove != null)
        {
            EntityCollider.Remove(entityToRemove);
        }
    }
    void IContent.Initialize()
    {
    }
    void IContent.Inject(IDomain domain)
    {
        this._domain = domain;
    }
    void IContent.Start()
    {
    }
    void IContent.Stop()
    {
    }
    void IContent.Terminate()
    {
    }
}
