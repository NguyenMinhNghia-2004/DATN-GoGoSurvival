using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    public interface IViewResolver
    {
        IView GetView(object data, object displayContext);
    }

    public sealed class UIItemSpawnerGeneric : ViewT<IEnumerable<object>>
    {
        [SerializeField] private Transform parent;
        [SerializeField] private View viewPrefab;
        [Space, Header("Optional View Resolver")]
        [SerializeField] bool useViewResolver;
        [SerializeField] private Component viewResolver;

        List<IView> _views = new();

        public IReadOnlyList<IView> Views => _views;
        public Transform Parent => parent;

        protected override void OnSetup()
        {
            base.OnSetup();

            foreach (var data in Data)
            {
                var myViewPrefab = useViewResolver ? ((IViewResolver)viewResolver).GetView(data, null) : viewPrefab;
                IView inst = Instantiate(myViewPrefab as Component, parent) as IView;
                inst.Setup(data);
                _views.Add(inst);
            }
        }

        protected override void OnTeardown()
        {
            base.OnTeardown();

            for (int i = _views.Count - 1; i >= 0; i--)
            {
                IView v = _views[i];
                v.Teardown();
                Destroy((v as Component)?.gameObject);
            }
            _views.Clear();
        }
    }
}
