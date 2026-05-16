using System;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public abstract class BehaviorBase : IBehavior
    {
        public IDomain MyDomain => Owner.MyDomain;
        public IEntity Owner { get; }
        public BehaviorBase(IEntity owner)
        {
            Owner = owner;
        }
        //void IBehavior.Initialize()
        //{
        //    DoInitialize();
        //}
        void IBehavior.Start()
        {
            DoStart();
        }
        void IBehavior.Update(float dt)
        {
            DoUpdate(dt);
        }
        void IBehavior.OnDestroy()
        {
            DoDestroy();
        }
        //protected virtual void DoInitialize() { }
        protected virtual void DoStart() { }
        protected virtual void DoUpdate(float dt) { }
        protected virtual void DoDestroy() { }
    }
}