using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Luzart
{
    public class HPVisualPlayer : AbstractMonoBehaviorContent, IEntityBehaviorProvider
    {
        [SerializeField] private Transform _hpBarTransform;
        private IEntity _playerEntity;
        private EntityBluePrint _entityBluePrint;
        private StatsBehavior _statsBehavior;
        private INumberWithSet HP => _statsBehavior.GetRuntime(StatType.Runtime_HP);
        public void SetHPBarFillAmount(float amount)
        {
            if(_hpBarTransform != null)
            {
                var localScale = _hpBarTransform.localScale;
                localScale.x = Mathf.Clamp01(amount);
                _hpBarTransform.localScale = localScale;
            }
        }
        void IEntityBehaviorProvider.CreateBehavior(IEntity entity)
        {
            this._playerEntity = entity;
            this._statsBehavior = entity.GetBehavior<StatsBehavior>();
            this.HP.Changed += HP_Changed;
        }
        private void HP_Changed(INumber obj)
        {
            float hpPercent = 1;
            if(_statsBehavior != null && _statsBehavior.Get(StatType.HPMax) !=null &&_statsBehavior.Get(StatType.HPMax).Value > 0)
            {
                hpPercent =(float)(HP.Value / _statsBehavior.Get(StatType.HPMax).Value);
            }
            SetHPBarFillAmount(hpPercent);
        }
        void IEntityBehaviorProvider.InitEntityBluePrint(EntityBluePrint entity)
        {
            this._entityBluePrint = entity;
        }
        public override void DoTerminate()
        {
            base.DoTerminate();
            if(_statsBehavior != null)
            {
                HP.Changed -= HP_Changed;
            }
        }
    }
}
