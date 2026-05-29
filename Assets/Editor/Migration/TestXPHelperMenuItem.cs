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
            var js = Object.FindFirstObjectByType<movementJoystick>(FindObjectsInactive.Include);
            if (js == null) { Debug.LogWarning("[TestXPHelper] movementJoystick not found in scene."); return; }
            // movementJoystick is on Joystick Table itself, so js.gameObject is the
            // GO we need to activate (not its parent which is _NinjaUI/2_Hud).
            js.gameObject.SetActive(true);
            // Also activate any inactive ancestor chain up to root so it shows.
            var t = js.transform.parent;
            while (t != null)
            {
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t = t.parent;
            }
            Debug.Log($"[TestXPHelper] Activated {js.gameObject.name} + ancestor chain. activeInHierarchy={js.gameObject.activeInHierarchy}");
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
