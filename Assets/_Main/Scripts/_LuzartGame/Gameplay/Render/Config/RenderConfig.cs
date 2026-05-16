using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "RenderConfig", menuName = "Luzart/Render/RenderConfig", order = 0)]
    public class RenderConfig : EntityConfigScriptableObject
    {
        [Header("Render Config")]
        [SerializeField] private AnimationConfig animationConfig;
        public AnimationConfig AnimationConfig => animationConfig;
        public RenderEntity CreateRender(IDomain domain,Vector2 vt2)
        {
            var render =  new RenderEntity(this, vt2);
            render.Inject(domain);
            render.Initialize();
            render.Start();
            return render;
        }
    }
}