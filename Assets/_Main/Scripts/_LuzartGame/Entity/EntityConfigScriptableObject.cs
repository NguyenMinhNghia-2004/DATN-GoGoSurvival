using System;
using UnityEngine;
namespace Luzart
{
    public class EntityConfigScriptableObject: AbstractScriptableContent
    {
        [Header("Visual Entity")]
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Material _material;
        [SerializeField] private Vector3 _scale = Vector3.one;
        [Space, Header("Infor Entity")]
        [SerializeField] protected float _lifeTime = 10000f;
        // Runtime material cache
        protected Material _runtimeMaterial;
        public Material Material
        {
            get
            {
                if (_runtimeMaterial == null && _material != null)
                {
                    _runtimeMaterial = new Material(_material);
                    if (_sprite != null)
                        _runtimeMaterial.mainTexture = _sprite.texture;
                }
                return _runtimeMaterial;
            }
        }
        public virtual Sprite Sprite => _sprite;
        public virtual float LifeTime => _lifeTime;
        public virtual Vector3 Scale => _scale;
#if UNITY_EDITOR
        public void Click()
        {
            _material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_GameSurvivorIO/Material/MaterialDraw.mat");
            Debug.Log("Auto Get Material for EnemyDefinition: ");
        }
#endif
    }
}