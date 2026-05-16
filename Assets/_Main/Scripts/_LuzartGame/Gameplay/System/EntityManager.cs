using Luzart;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EntityManager : AbstractMonoBehaviorContent
{
    private List<EntityBase> _entities = new List<EntityBase>();
    public void Add(EntityBase entity)
    {
        if (!this._entities.Contains(entity))
        {
            this._entities.Add(entity);
        }
    }
    public void Remove(EntityBase entity)
    {
        if (this._entities.Contains(entity))
        {
           this._entities.Remove(entity);
        }
    }
    private void OnDestroy()
    {
        DoStop();
        DoTerminate();
    }
    public override void DoStop()
    {
        base.DoStop();
        int length = _entities.Count;
        for (int i = 0; i < length; i++)
        {
            EntityBase entity = _entities[i];
            entity.Stop();
        }
    }
    public override void DoTerminate()
    {
        base.DoTerminate();
        int length = _entities.Count;
        for (int i = 0; i < length; i++)
        {
            EntityBase entity = _entities[i];
            entity.Terminate();
        }
    }
    protected override void DoUpdate(float dt)
    {
        base.DoUpdate(dt);
        int length = _entities.Count;
        for (int i = 0; i < length; i++)
        {
            EntityBase entity = _entities[i];
            if(entity == null)
            {
                _entities.RemoveAt(i);
                i--;
                length--;
                continue;
            }
            entity.OnUpdate(dt);
            if (entity.IsDead)
            {
                entity.Stop();
                entity.Terminate();
                _entities.RemoveAt(i);
                i--;
                length--;
            }
        }
    }
}
