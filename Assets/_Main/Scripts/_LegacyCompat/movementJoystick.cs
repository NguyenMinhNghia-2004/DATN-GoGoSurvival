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
        // Travel radius = a fraction of bg half-size so handle stays visually inside the ring.
        // Matches survivor.io feel (~1/3 of full diameter).
        var size = _bgRect.rect.size;
        _radiusPx = Mathf.Min(size.x, size.y) * 0.5f * 0.85f;
        if (_radiusPx <= 0f) _radiusPx = 100f;
    }

    // ---- Event handlers (wired by scene EventTrigger via SendMessage style) -
    public void PointerDown(BaseEventData _)
    {
        // Legacy implementation re-centered handle and started tracking; keep handle
        // pinned to center until first Drag updates it.
        joystickVec = Vector2.zero;
        if (_handleRect != null) _handleRect.anchoredPosition = Vector2.zero;
    }

    public void Drag(BaseEventData data)
    {
        if (_bgRect == null || _handleRect == null) return;
        var ptr = data as PointerEventData;
        if (ptr == null) return;

        // Convert screen pos → bg local pos. Use canvas camera for ScreenSpace-Camera/World;
        // null for ScreenSpace-Overlay (Unity convention).
        Camera cam = _parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _parentCanvas.worldCamera : null;
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_bgRect, ptr.position, cam, out localPoint))
            return;

        // Clamp offset within travel radius.
        Vector2 clamped = Vector2.ClampMagnitude(localPoint, _radiusPx);
        _handleRect.anchoredPosition = clamped;
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

    public void PointerUp(BaseEventData _)
    {
        joystickVec = Vector2.zero;
        if (_handleRect != null) _handleRect.anchoredPosition = Vector2.zero;
    }
}
