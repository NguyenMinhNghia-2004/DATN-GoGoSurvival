using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Post-nuke (W4) restoration of the legacy <c>movementJoystick</c> MonoBehaviour.
/// The original .cs was deleted; the scene's Joystick Table still has a Missing Script
/// component (guid <c>7166dea7049259b45a1786e00b1a7943</c>) with its serialized fields
/// (<c>joystick</c>, <c>joystickBG</c>, <c>joystickVec</c>, <c>Gun</c>, <c>ArrowDirecteur</c>)
/// and an EventTrigger wiring PointerDown/Drag/PointerUp to methods on this type.
///
/// This file restores the class behind the SAME guid (see .cs.meta) so Unity auto-resolves
/// the Missing Script reference without any scene edits. <see cref="LuzartPlayerController"/>
/// reads <c>joystickVec</c> via reflection — keeping the field name + accessibility public
/// preserves the reflection contract.
///
/// Visual: handle (assigned to <c>joystick</c>) drags within the background (<c>joystickBG</c>)
/// radius; <c>joystickVec</c> exposes the normalized offset in [-1, 1] for both axes.
/// </summary>
public class movementJoystick : MonoBehaviour
{
    // ---- Serialized fields (names match scene YAML, do not rename) ---------
    [Tooltip("Optional arrow indicator that rotates to match drag direction (legacy parity).")]
    public GameObject ArrowDirecteur;
    [Tooltip("Optional weapon root whose facing flips with horizontal drag (legacy parity).")]
    public GameObject Gun;
    [Tooltip("Draggable handle (child UI image). Snaps back to center on release.")]
    public GameObject joystick;
    [Tooltip("Static background image. Its RectTransform defines the drag radius.")]
    public GameObject joystickBG;
    [Tooltip("Output: normalized joystick vector in range [-1, 1]. Consumed by LuzartPlayerController via reflection.")]
    public Vector2 joystickVec;

    // ---- Internals ---------------------------------------------------------
    private RectTransform _bgRect;
    private RectTransform _handleRect;
    private Canvas _parentCanvas;
    private float _radiusPx; // max handle travel from bg center, in canvas pixels

    private void Awake()
    {
        if (joystickBG != null) _bgRect = joystickBG.GetComponent<RectTransform>();
        if (joystick != null) _handleRect = joystick.GetComponent<RectTransform>();
        _parentCanvas = GetComponentInParent<Canvas>();
        RecomputeRadius();
    }

    private void OnEnable()
    {
        // Reset state so re-activating mid-run starts clean.
        joystickVec = Vector2.zero;
        if (_handleRect != null) _handleRect.anchoredPosition = Vector2.zero;
        RecomputeRadius();
    }

    private void RecomputeRadius()
    {
        if (_bgRect == null) { _radiusPx = 100f; return; }
        // Use sizeDelta (Inspector value, always present) — rect.size returns 0 in Awake
        // before the first layout pass, which would zero out joystickVec at runtime.
        var size = _bgRect.sizeDelta;
        if (size.x <= 0f || size.y <= 0f) size = _bgRect.rect.size; // fallback for stretched anchors
        _radiusPx = Mathf.Min(size.x, size.y) * 0.5f * 0.85f;
        if (_radiusPx <= 0f) _radiusPx = 100f;
        // Handle is a SIBLING of bg (both children of Joystick Table) with the same anchor.
        // To make handle visually orbit bg, base its anchoredPosition off bg's anchoredPosition.
        if (_bgRect != null) _bgAnchoredHome = _bgRect.anchoredPosition;
    }

    private Vector2 _bgAnchoredHome;

    // ---- Event handlers — Void mode (no args) overloads are required by the scene's
    //      EventTrigger entries with m_Mode: 1. The (BaseEventData) overloads are kept
    //      for any caller that uses EventDefined mode (m_Mode: 0).
    public void PointerDown() { PointerDownCore(); }
    public void PointerDown(BaseEventData _) => PointerDownCore();
    private void PointerDownCore()
    {
        // Re-center handle and clear vec until first Drag updates them.
        joystickVec = Vector2.zero;
        if (_handleRect != null) _handleRect.anchoredPosition = _bgAnchoredHome;
        if (_radiusPx <= 0f) RecomputeRadius(); // handle late-layout fallback
    }

    public void Drag(BaseEventData data)
    {
        if (_bgRect == null || _handleRect == null) return;
        var ptr = data as PointerEventData;
        if (ptr == null) return;
        if (_radiusPx <= 0f) RecomputeRadius();

        // Convert screen pos → bg local pos. Use canvas camera for ScreenSpace-Camera/World;
        // null for ScreenSpace-Overlay (Unity convention).
        Camera cam = _parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _parentCanvas.worldCamera : null;
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_bgRect, ptr.position, cam, out localPoint))
            return;

        // Clamp offset within travel radius.
        Vector2 clamped = Vector2.ClampMagnitude(localPoint, _radiusPx);
        // Handle is a sibling of bg with same anchor — orbit it around bg by offsetting
        // from bg's anchoredPosition. NOT bare `clamped` (which would jump handle 552 px
        // off, since bg sits at anchoredPosition (0, -552) within Joystick Table).
        _handleRect.anchoredPosition = _bgAnchoredHome + clamped;
        joystickVec = _radiusPx > 0f ? clamped / _radiusPx : Vector2.zero;

        // Optional facing aids (legacy parity — no-op when refs unassigned).
        if (Gun != null)
        {
            var s = Gun.transform.localScale;
            float mag = Mathf.Abs(s.x);
            if (joystickVec.x < 0f) Gun.transform.localScale = new Vector3(-mag, s.y, s.z);
            else if (joystickVec.x > 0f) Gun.transform.localScale = new Vector3(mag, s.y, s.z);
        }
        if (ArrowDirecteur != null && joystickVec.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(joystickVec.y, joystickVec.x) * Mathf.Rad2Deg;
            ArrowDirecteur.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

    public void PointerUp() { PointerUpCore(); }
    public void PointerUp(BaseEventData _) => PointerUpCore();
    private void PointerUpCore()
    {
        joystickVec = Vector2.zero;
        if (_handleRect != null) _handleRect.anchoredPosition = _bgAnchoredHome;
    }
}
