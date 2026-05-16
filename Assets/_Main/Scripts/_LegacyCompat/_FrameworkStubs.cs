// Minimal stubs for framework types that originally lived in deleted UI/View folder.
// Allows Cost/, Item/, MonoBehavior/ code to compile while NinjaUI migration is in progress.
// To be replaced/removed when each subsystem migrates to NinjaUI explicitly.

using UnityEngine;

namespace Luzart
{
    // ============================================================
    // View / UI primitives that lived in old `LuzartTechnical/View`
    // and `LuzartTechnical/UIItem` folders. Stubbed to keep referencing
    // code compiling. Real visual is delegated to NinjaUI's UIBase.
    // ============================================================

    /// <summary>Marker for anything that can be "rendered" as UI. Replaced by NinjaUI UIBase later.</summary>
    public interface IView { }

    /// <summary>Strongly-typed View placeholder, originally referenced via .asset assignment.</summary>
    public class ViewT<T> : ScriptableObject, IView { }

    /// <summary>Generic UI representation of a domain object — originally a MonoBehaviour pool.</summary>
    public class ObjectView : MonoBehaviour, IView { }

    // ============================================================
    // Cost system visual adapters
    // ============================================================

    /// <summary>
    /// Original: UI adapter that rendered a cost into an `IView`.
    /// Now: just an interface marker — Cost data still flows through gameplay code,
    /// but visual rendering is delegated to NinjaUI screens that consume the cost data.
    /// </summary>
    public interface ICostVisualResolver
    {
        IView GetCostView(ICost data, object displayContext);
    }

    /// <summary>Stub keep existing ResourceDefinition.asset field shape valid.</summary>
    public class AssetCostVisualResolver_ResourcePool : AbstractScriptableContent, ICostVisualResolver
    {
        [SerializeField] private ViewT<IResourceCost> resourceCostView_singleLine;
        IView ICostVisualResolver.GetCostView(ICost data, object displayContext)
        {
            return resourceCostView_singleLine;
        }
    }

    /// <summary>Display-context enum kept for compatibility with cost-display code.</summary>
    public static class EResourceCostView
    {
        public class SingleLine { }
    }

    // ============================================================
    // Broadcaster payloads from old UI joystick.
    // ============================================================

    /// <summary>
    /// Broadcaster payload originally raised by old JoystickControllerView UI.
    /// Stubbed so `MoveMonoBehavior` (gameplay-side listener) compiles.
    /// When migrating Joystick to NinjaUI, raise this struct with real Direction.
    /// </summary>
    public struct JoystickBroadcastData : IBroadcastData
    {
        public Vector2 Direction;
    }

    /// <summary>
    /// Game-end broadcast originally from `ClassicMode`. Kept so `GameController`
    /// can still register/raise the event. Wire into NinjaUI EndGame popup later.
    /// </summary>
    public struct Data_ClassicEndGame : IBroadcastData
    {
        public bool IsWin;
        public int FinalScore;
        public int EnemiesKilled;
        public float SurvivalTime;
    }

    /// <summary>
    /// Popup data for skill-upgrade roll. Wire into NinjaUI's skill-upgrade screen
    /// when migrating `UpgradeSkillManager` to NinjaUI flow.
    /// </summary>
    public class PopupSkillUpgradeData
    {
        public System.Collections.Generic.List<Data_UpgradeSkill> Options;
        public void InitData(System.Collections.Generic.List<Data_UpgradeSkill> options) => Options = options;
    }

    // Data_UpgradeSkill and ListExtensions.GetShuffle live in `_LuzartGame/...` — not stubbed here.
}
