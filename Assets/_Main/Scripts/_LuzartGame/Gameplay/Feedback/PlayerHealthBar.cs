using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// World-space health bar that floats above the player's head. Lives on the Player GameObject
    /// (auto-added by <see cref="LuzartPlayerEntityRoot"/>) so it spawns/despawns with the player.
    ///
    /// <para>Renders with two <see cref="SpriteRenderer"/>s (background + colored fill) at a fixed
    /// world offset above the player — NOT parented to the player, so the player's facing flip
    /// (localScale.x = -1) doesn't mirror the bar. The fill shrinks from the left and lerps
    /// green→red as HP drops. Reads HP from the canonical <see cref="PlayerCharacter"/> in the
    /// Domain (resolved lazily, re-resolves cleanly on a per-run player respawn).</para>
    /// </summary>
    public sealed class PlayerHealthBar : MonoBehaviour
    {
        private const float Width = 1.1f;
        private const float Height = 0.14f;
        private const float OffsetY = 0.85f;

        private StatsBehavior _stats;
        private INumber _hp;
        private bool _subscribed;

        private Transform _container; // separate root object that follows the player
        private Transform _fill;
        private SpriteRenderer _fillSr;

        private void Start() => BuildVisual();

        private void OnDestroy()
        {
            if (_subscribed && _hp != null) _hp.Changed -= OnHpChanged;
            if (_container != null) Destroy(_container.gameObject);
        }

        private void Update()
        {
            if (_stats == null) Resolve();
        }

        private void LateUpdate()
        {
            if (_container != null)
                _container.position = transform.position + new Vector3(0f, OffsetY, 0f);
        }

        private void Resolve()
        {
            var domain = SceneRootManager.Instance != null ? SceneRootManager.Instance.Domain : null;
            var pc = domain != null ? domain.Get<PlayerCharacter>() : null;
            if (pc == null || pc.Stats == null) return;
            _stats = pc.Stats;
            _hp = _stats.GetRuntime(StatType.Runtime_HP);
            if (_hp != null) { _hp.Changed += OnHpChanged; _subscribed = true; }
            Refresh();
        }

        private void OnHpChanged(INumber n) => Refresh();

        private void Refresh()
        {
            if (_stats == null || _fill == null || _hp == null) return;
            double max = _stats.Get(StatType.HPMax).Value;
            float ratio = max > 0 ? Mathf.Clamp01((float)(_hp.Value / max)) : 0f;
            _fill.localScale = new Vector3(Width * ratio, Height, 1f);
            if (_fillSr != null) _fillSr.color = Color.Lerp(Color.red, Color.green, ratio);
        }

        private void BuildVisual()
        {
            var center = WhiteSprite(new Vector2(0.5f, 0.5f));
            var left = WhiteSprite(new Vector2(0f, 0.5f)); // left pivot → shrinks from the left

            _container = new GameObject("PlayerHealthBar_Visual").transform;
            _container.position = transform.position + new Vector3(0f, OffsetY, 0f);

            var bg = new GameObject("BG", typeof(SpriteRenderer));
            bg.transform.SetParent(_container, false);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(Width, Height, 1f);
            var bgSr = bg.GetComponent<SpriteRenderer>();
            bgSr.sprite = center;
            bgSr.color = new Color(0f, 0f, 0f, 0.7f);
            bgSr.sortingOrder = 4000;

            var fill = new GameObject("Fill", typeof(SpriteRenderer));
            fill.transform.SetParent(_container, false);
            fill.transform.localPosition = new Vector3(-Width * 0.5f, 0f, 0f);
            fill.transform.localScale = new Vector3(Width, Height, 1f);
            _fillSr = fill.GetComponent<SpriteRenderer>();
            _fillSr.sprite = left;
            _fillSr.color = Color.green;
            _fillSr.sortingOrder = 4001;
            _fill = fill.transform;

            Refresh();
        }

        private static Sprite WhiteSprite(Vector2 pivot)
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, tex.width);
        }
    }
}
