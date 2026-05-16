using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    public class DropBehavior : BehaviorBase
    {
        private DropConfigRequire _dropConfigRequire;
        private EntityManager _entityManager;
        private StatsBehavior _stats;
        public DropBehavior(IEntity owner) : base(owner)
        {
        }
        public void Configure(DropConfigRequire dropConfig)
        {
            _dropConfigRequire = dropConfig;
        }
        protected override void DoStart()
        {
            base.DoStart();
            _entityManager = MyDomain.Get<EntityManager>();
            _stats = Owner.GetBehavior<StatsBehavior>();
            _stats.GetRuntime(StatType.Runtime_HP).Changed += OnHPChanged;
        }
        protected override void DoDestroy()
        {
            base.DoDestroy();
            _stats.GetRuntime(StatType.Runtime_HP).Changed -= OnHPChanged;
        }
        private bool _hasDropped = false;
        private void OnHPChanged(INumber obj)
        {
            if( obj.Value <= 0 && !_hasDropped)
            {
                HandleDrop();
                _hasDropped = true;
            }
        }
        private void HandleDrop()
        {
            List<DropConfigRequireItem> dropItems = _dropConfigRequire.DropConfigRequireItems;
            int length = dropItems.Count;
            for (int i = 0; i < length; i++)
            {
                var dropCondfigRequireItem = dropItems[i];
                for (int j = 0; j < dropCondfigRequireItem.amount; j++)
                {
                    var dropConfig = dropCondfigRequireItem.dropConfig;
                    var drop = dropConfig.CreateDrop(Owner);
                    drop.Inject(MyDomain);
                    drop.Initialize();
                    drop.ConfigTransform(Owner.Transform.Position.Value);
                    drop.Start();
                    _entityManager.Add(drop);
                }
            }
        }
    }
}