using Luzart;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.UIElements;
namespace Luzart
{
    public class EnemyCharacter : CharacterBase
    {
        private Material material;
        private ColliderBehavior _colliderBehavior;
        public EnemyCharacter(Material mat, string idEnemy)
        {
            Id = idEnemy;
            this.material = mat;
        }
        public override void Inject(IDomain domain)
        {
            base.Inject(domain);
        }
        public override void Initialize()
        {
            base.Initialize();
            // Add core behaviors
            //AddBehavior(new MoveBehavior(this));
            AddBehavior(new SkillControllerBehavior(this));
            AddBehavior(new EnemyAIBehavior(this));
            AddBehavior(new DropBehavior(this));
            var renderBehavior = new RenderBehavior(this);
            // Configure với Enemy sorting layer
            renderBehavior.Configure(material, SortingLayerRender.Enemies, 0);
            AddBehavior(renderBehavior);
            var animationBehavior = new AnimationBehavior(this);
            AddBehavior(animationBehavior);
            _colliderBehavior = new ColliderBehavior(this, SpatialLayer.Enemies, radius: 0.5f);
            AddBehavior(_colliderBehavior);
            var collisionHandler = new EnemyCollisionHandlerBehavior(this);
            AddBehavior(collisionHandler);
        }
        public override void Start()
        {
            base.Start();
            Stats.GetRuntime(StatType.Runtime_HP).Changed += OnHPChanged;
        }
        private void OnHPChanged(INumber value)
        {
            if (value.Value <= 0 && !_isDead)
            {
                OnDeath();
            }
        }
        public override void Stop()
        {
            Stats.GetRuntime(StatType.Runtime_HP).Changed -= OnHPChanged;
            base.Stop();
        }
        #region Configuration Methods (Called by EnemyManager using EnemyDefinition)
        public void ConfigureEnemyFromDefinition(EnemyDefinition def)
        {
            Transform.SetScale(def.Scale);
            ConfigureRendering(material);
            ConfigureAnimation(def.AnimationConfig);
            ConfigureSkills(def.SkillConfig);
        }
        public void ConfigureRendering(Material mat)
        {
            var renderBehavior = GetBehavior<RenderBehavior>();
            if (renderBehavior != null)
            {
                renderBehavior.SetMaterial(mat);
            }
        }
        public void ConfigureDrop(DropConfigRequire dropConfig)
        {
            var dropBehavior = GetBehavior<DropBehavior>();
            if (dropBehavior != null)
            {
                dropBehavior.Configure(dropConfig);
            }
        }
        /// <summary>
        /// Configure animation từ EnemyDefinition
        /// </summary>
        public void ConfigureAnimation(AnimationConfig animationConfig)
        {
            var animBehavior = GetBehavior<AnimationBehavior>();
            if (animBehavior != null)
            {
                animBehavior.Configure(animationConfig);
            }
        }
        public void ConfigureSkills(List<ZSkillConfig> skillConfigs)
        {
            var skillController = GetBehavior<SkillControllerBehavior>();
            if (skillController != null)
            {
                skillController.InitZSkillConfigs(skillConfigs);
            }
        }
        #endregion
    }
}
