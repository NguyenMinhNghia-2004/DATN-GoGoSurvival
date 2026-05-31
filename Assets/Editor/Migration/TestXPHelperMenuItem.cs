#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Luzart;

namespace Luzart.Migration.EditorTools
{
    /// <summary>
    /// Test helper — grant XP to the live PlayerCharacter to verify Phase F level-up
    /// pipeline (CachePolicy ReleaseOnClose + ShowLevelUpPopupSafe). Use from Play
    /// mode. Each Grant fires Stats.Runtime_XP.Set → GameController.OnXPChange →
    /// _currentLevel.Set → UpgradeSkillManager.UpgradeLevel → ShowAsync popup.
    /// </summary>
    public static class TestXPHelperMenuItem
    {
        [MenuItem("Tools/Migration/Test — Start Gameplay (subscribe XP/HP)")]
        public static void StartGameplayMenu()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[TestXPHelper] Enter Play mode first."); return; }
            var srm = SceneRootManager.Instance;
            var gc = srm != null ? srm.Domain?.Get<GameController>() : null;
            if (gc == null) { Debug.LogWarning("[TestXPHelper] GameController null."); return; }

            // Match the full MainMenu Play-button flow so MapReady flips + Joystick Table
            // activates. Without this, LuzartPlayerController.Update gates early (MapReady=false)
            // and the joystick input never reaches the player.
            var legacy = Object.FindFirstObjectByType<DATN.Legacy.UIManager>();
            if (legacy != null)
            {
                legacy.PlayBtn();
                Debug.Log("[TestXPHelper] legacy UIManager.PlayBtn() called — MapReady will flip after 0.7s.");
            }
            else
            {
                Debug.LogWarning("[TestXPHelper] DATN.Legacy.UIManager not found — MapReady won't flip.");
            }

            gc.StartGameplay();
            Debug.Log("[TestXPHelper] StartGameplay called — OnXPChange now subscribed.");

            // Show NinjaUI HUD so Joystick Table gets EnableLegacyJoystick(true).
            var ninjaUI = Luzart.UIManager.Instance;
            if (ninjaUI != null)
            {
                ninjaUI.ShowAsync(Luzart.UIId.SV_GameplayHud).Forget();
                Debug.Log("[TestXPHelper] ShowAsync(SV_GameplayHud) requested.");
            }
        }

        [MenuItem("Tools/Migration/Test — Force-activate Joystick Table (manual)")]
        public static void ForceActivateJoystick()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[TestXPHelper] Enter Play mode first."); return; }
            // W4 nuke: legacy movementJoystick class deleted — resolve Joystick Table by canonical scene path.
            var jt = GameObject.Find("/_NinjaUI/2_Hud/Joystick Table");
            if (jt == null) { Debug.LogWarning("[TestXPHelper] Joystick Table not found at /_NinjaUI/2_Hud/Joystick Table."); return; }
            jt.SetActive(true);
            var t = jt.transform.parent;
            while (t != null)
            {
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t = t.parent;
            }
            Debug.Log($"[TestXPHelper] Activated {jt.name} + ancestor chain. activeInHierarchy={jt.activeInHierarchy}");
        }

        [MenuItem("Tools/Migration/Test — Force CurrentLevel +1 (skip XP, fast popup test)")]
        public static void ForceLevelUp()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[TestXPHelper] Enter Play mode first."); return; }
            var srm = SceneRootManager.Instance;
            var gc = srm != null ? srm.Domain?.Get<GameController>() : null;
            if (gc == null) { Debug.LogWarning("[TestXPHelper] GameController null."); return; }
            int cur = gc.CurrentLevel.Value;
            gc.CurrentLevel.Set(cur + 1);
            Debug.Log($"[TestXPHelper] Forced CurrentLevel {cur} → {cur + 1}.");
        }

        [MenuItem("Tools/Migration/Test — Grant 50 XP (Play mode)")]
        public static void GrantXP50()
        {
            GrantXp(50);
        }

        [MenuItem("Tools/Migration/Test — Grant 200 XP (Play mode)")]
        public static void GrantXP200()
        {
            GrantXp(200);
        }

        [MenuItem("Tools/Migration/Test — Auto-pick first option (unstuck queue)")]
        public static void AutoPickFirstOption()
        {
            // Force a broadcast to flush the queue if a popup got stuck.
            // No-op if there's nothing in flight.
            var dummy = new SkillUpgradeSuccessBroadcastData(null, 0);
            try { Broadcaster.Broadcast(dummy); } catch { /* swallow */ }
            Debug.Log("[TestXPHelper] Dummy broadcast sent to unstuck the queue.");
        }

        [MenuItem("Tools/Migration/Diag — Dump runtime state (HP/MapReady/Joystick/UI)")]
        public static void DumpRuntimeState()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[Diag] Enter Play mode first."); return; }
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("===== RUNTIME STATE DUMP =====");
            sb.AppendLine("Time.timeScale=" + Time.timeScale + " isPaused=" + UnityEditor.EditorApplication.isPaused);

            var srm = SceneRootManager.Instance;
            sb.AppendLine("SRM=" + (srm != null) + " Domain=" + (srm != null && srm.Domain != null));
            if (srm != null && srm.Domain != null)
            {
                var flags = srm.Domain.Get<Migration.MigrationFlags>();
                sb.AppendLine("Flags=" + (flags != null)
                    + " LPC=" + (flags != null && flags.UseLuzartPlayerController)
                    + " LPER=" + (flags != null && flags.UseLuzartPlayerEntityRoot)
                    + " LEER=" + (flags != null && flags.UseLuzartEnemyEntityRoot));
                var gc = srm.Domain.Get<GameController>();
                sb.AppendLine("GC=" + (gc != null) + " MapReady=" + (gc != null && gc.MapReady));
                var pc = srm.Domain.Get<PlayerCharacter>();
                sb.AppendLine("PC=" + (pc != null) + " type=" + (pc != null ? pc.GetType().Name : "n/a"));
                if (pc != null && pc.Stats != null)
                {
                    var hp = pc.Stats.GetRuntime(StatType.Runtime_HP);
                    var xp = pc.Stats.GetRuntime(StatType.Runtime_XP);
                    sb.AppendLine("HP=" + (hp != null ? hp.Value.ToString() : "null") + " XP=" + (xp != null ? xp.Value.ToString() : "null"));
                }
            }

            // LuzartPlayerController
            var lpc = Object.FindFirstObjectByType<LuzartPlayerController>();
            sb.AppendLine("LPC=" + (lpc != null));
            if (lpc != null)
            {
                sb.AppendLine("  GO=" + lpc.gameObject.name + " active=" + lpc.gameObject.activeInHierarchy + " enabled=" + lpc.enabled);
                var t = typeof(LuzartPlayerController);
                var fJs = t.GetField("_joystick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var js = fJs != null ? fJs.GetValue(lpc) as MonoBehaviour : null;
                if (js == null) sb.AppendLine("  _joystick=NULL");
                else
                {
                    sb.AppendLine("  _joystick=" + js.GetType().Name + " on '" + js.gameObject.name + "' active=" + js.gameObject.activeInHierarchy + " enabled=" + js.enabled);
                    var fVec = js.GetType().GetField("joystickVec", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    sb.AppendLine("  joystickVec=" + (fVec != null ? fVec.GetValue(js).ToString() : "(no field)"));
                }
                var fRb = t.GetField("_rb", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var rb = fRb != null ? fRb.GetValue(lpc) as Rigidbody2D : null;
                sb.AppendLine("  _rb=" + (rb != null ? "v=" + rb.linearVelocity + " bodyType=" + rb.bodyType + " simulated=" + rb.simulated : "NULL"));
            }

            // Joystick discovery — W4 nuke: legacy movementJoystick class deleted, just check canonical path.
            var jtDiag = GameObject.Find("/_NinjaUI/2_Hud/Joystick Table");
            sb.AppendLine("JoystickTable: " + (jtDiag != null ? $"found, active={jtDiag.activeInHierarchy}" : "NOT FOUND at canonical path"));

            // UI Canvas
            var uim = UIManager.Instance;
            sb.AppendLine("UIManager=" + (uim != null));
            if (uim != null)
            {
                var c = uim.GetComponent<Canvas>();
                sb.AppendLine("  Canvas enabled=" + (c != null && c.enabled) + " renderMode=" + (c != null ? c.renderMode.ToString() : "n/a"));
                var rt = uim.transform as RectTransform;
                sb.AppendLine("  RT lossyScale=" + (rt != null ? rt.lossyScale.ToString() : "n/a") + " localScale=" + (rt != null ? rt.localScale.ToString() : "n/a"));
                sb.AppendLine("  childCount=" + uim.transform.childCount + " activeInHier=" + uim.gameObject.activeInHierarchy);
            }

            // EventSystem
            var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            sb.AppendLine("EventSystem=" + (es != null) + " enabled=" + (es != null && es.enabled));

            sb.AppendLine("===== END DUMP =====");
            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Migration/Test — Click first LevelUp slot (simulate pick)")]
        public static void ClickFirstSlot()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[TestXPHelper] Enter Play mode first."); return; }
            var popups = Object.FindObjectsByType<SV_LevelUpPopupUI>(FindObjectsSortMode.None);
            if (popups == null || popups.Length == 0) { Debug.LogWarning("[TestXPHelper] No SV_LevelUpPopupUI in scene."); return; }
            var popup = popups[0];
            // Find first Button child and Invoke its onClick.
            var btn = popup.GetComponentInChildren<UnityEngine.UI.Button>(true);
            if (btn == null) { Debug.LogWarning("[TestXPHelper] No Button in popup."); return; }
            btn.onClick.Invoke();
            Debug.Log($"[TestXPHelper] Invoked onClick on first Button: {btn.name}");
        }

        private static void GrantXp(double amount)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TestXPHelper] Enter Play mode first.");
                return;
            }
            var srm = SceneRootManager.Instance;
            if (srm == null || srm.Domain == null)
            {
                Debug.LogWarning("[TestXPHelper] SceneRootManager/Domain not ready.");
                return;
            }
            var player = srm.Domain.Get<PlayerCharacter>();
            if (player == null || player.Stats == null)
            {
                Debug.LogWarning("[TestXPHelper] PlayerCharacter or Stats null.");
                return;
            }
            player.Stats.AddXP(amount);
            Debug.Log($"[TestXPHelper] Granted {amount} XP. Runtime_XP now: {player.Stats.GetRuntime(StatType.Runtime_XP).Value}");
        }
    }
}
#endif
