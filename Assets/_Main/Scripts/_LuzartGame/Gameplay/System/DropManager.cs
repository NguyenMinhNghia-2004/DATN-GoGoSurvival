using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace Luzart
{
    public class DropManager : MonoBehaviour, IContent
    {
        string IContent.Id => "DropManager";
        private IDomain _domain;
        IDomain IContent.MyDomain => _domain;
        private List<DropEntity> _dropEntitys = new();
        void IContent.Initialize()
        {
            _dropEntitys.Clear();
        }
        void IContent.Inject(IDomain domain)
        {
            _domain = domain;
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
        private void Update()
        {
            float dt = Time.deltaTime;
            int length = _dropEntitys.Count;
            for (int i = 0; i < length; i++)
            {
                var drop = _dropEntitys[i];
                drop.OnUpdate(dt);
                if (drop.IsDead)
                {
                    _dropEntitys.RemoveAt(i);
                    i--;
                    length--;
                }
            }
        }
        public DropEntity SpawnDrop(DropConfig dropConfig, IEntity owner)
        {
            if (dropConfig == null || owner == null)
            {
                return null;
            }
            var drop = dropConfig.CreateDrop(owner);
            if (drop == null)
            {
                return null;
            }
            drop.Inject(_domain);
            drop.Initialize();
            drop.ConfigTransform(owner.Transform.Position.Value);
            drop.Start();
            _dropEntitys.Add(drop);
            return drop;
        }
        public void DespawnDrop(DropEntity drop)
        {
            drop.Stop();
            drop.Terminate();
        }
    }
}