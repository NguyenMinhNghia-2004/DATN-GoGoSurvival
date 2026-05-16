using UnityEngine;
namespace Luzart
{
    public class EntityBluePrint: AbstractMonoBehaviorContent
    {
        [SerializeField] private AbstractMonoBehaviorContent[] _monoBehaviours;
        [SerializeField] private StatsConfig _statPlayer;
        public StatsConfig statsConfig => _statPlayer;
        private PlayerCharacter _playerCharacter;
        public override void DoInitialize()
        {
            base.DoInitialize();
            int length = _monoBehaviours.Length;
            for (int i = 0; i < length; i++)
            {
                if (_monoBehaviours[i] is IEntityBehaviorProvider provider)
                {
                    provider.CreateBehavior(_playerCharacter);
                    provider.InitEntityBluePrint(this);
                }
            }
        }
        public override void DoStart()
        {
            base.DoStart();
            int length = _monoBehaviours.Length;
            for (int i = 0; i < length; i++)
            {
                _monoBehaviours[i].DoInitialize();
                _monoBehaviours[i].DoStart();
            }
        }
        public override void DoStop()
        {
            base.DoStop();
            int length = _monoBehaviours.Length;
            for (int i = 0; i < length; i++)
            {
                _monoBehaviours[i].DoStop();
                _monoBehaviours[i].DoTerminate();
            }
        }
        public override void DoInject(IDomain domain)
        {
            base.DoInject(domain);
            _playerCharacter = new PlayerCharacter(this);
            domain.Add<PlayerCharacter>(_playerCharacter);
        }
        protected override void DoUpdate(float dt)
        {
            base.DoUpdate(dt);
            _playerCharacter.OnUpdate(dt);
        }
        public override void DoTerminate()
        {
            base.DoTerminate();
            int length = _monoBehaviours.Length;
            for (int i = 0; i < length; i++)
            {
                if (_monoBehaviours[i] is IContent iContent)
                {
                    iContent.Terminate();
                }
            }
        }
    }
}