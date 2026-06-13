using TMPro;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Floating combat damage number. World-space 3D <see cref="TextMeshPro"/> that rises a little
    /// and fades out, then destroys itself. Spawned at the hit position by the damage-application
    /// sites:
    /// <list type="bullet">
    /// <item>enemy took damage → <see cref="LuzartEnemyEntityRoot.TakeDamage"/> (the single chokepoint
    /// every player skill/projectile/aura funnels through);</item>
    /// <item>player took damage → <see cref="LuzartPlayerController"/> enemy-touch.</item>
    /// </list>
    ///
    /// <para>No prefab / pool needed — it builds its own GameObject + TextMeshPro at runtime
    /// (mirrors how projectiles/diamonds use plain Instantiate). The game is 2D orthographic so no
    /// billboarding is required.</para>
    /// </summary>
    public sealed class DamageNumber : MonoBehaviour
    {
        private const float Lifetime = 0.7f;
        private const float RiseSpeed = 1.6f;   // world units / sec
        private const float DriftMax = 0.5f;    // random horizontal spread

        private TextMeshPro _tmp;
        private float _age;
        private Vector3 _velocity;

        /// <summary>Spawn a damage number at <paramref name="worldPos"/>. Color hints intent:
        /// e.g. white/yellow for damage to enemies, red for damage to the player.</summary>
        public static void Spawn(Vector3 worldPos, double amount, Color color)
        {
            // Round to an int for readability; never show 0 (a 0.5 touch still reads as "1").
            int shown = Mathf.Max(1, Mathf.RoundToInt((float)amount));

            var go = new GameObject("DamageNumber");
            go.transform.position = worldPos + new Vector3(Random.Range(-0.2f, 0.2f), 0.3f, 0f);
            go.transform.localScale = Vector3.one * 0.06f; // 3D TMP world units are large → scale down

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = shown.ToString();
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.enableWordWrapping = false;
            tmp.fontStyle = FontStyles.Bold;
            if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;

            // Render above world sprites (projectiles/enemies).
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 5000;

            var dn = go.AddComponent<DamageNumber>();
            dn._tmp = tmp;
            dn._velocity = new Vector3(Random.Range(-DriftMax, DriftMax), RiseSpeed, 0f);
        }

        private void Update()
        {
            // Use unscaled-independent dt; damage numbers should still animate at timeScale 1.
            float dt = Time.deltaTime;
            _age += dt;
            transform.position += _velocity * dt;

            if (_tmp != null)
            {
                float t = _age / Lifetime;
                var c = _tmp.color;
                c.a = Mathf.Clamp01(1f - t);
                _tmp.color = c;
            }

            if (_age >= Lifetime) Destroy(gameObject);
        }
    }
}
