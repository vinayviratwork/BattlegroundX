using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Full mobile input system:
///   - Virtual joystick (move)
///   - Swipe pad (look)
///   - Fire, Reload, Weapon Switch, Jump, Crouch, Aim, Medkit buttons
/// Wire each UI element in the Inspector or via the scene generator.
/// </summary>
public class MobileInputManager : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public static MobileInputManager Instance { get; private set; }

    // ── Joystick ──────────────────────────────────────────────────────────
    [Header("Joystick")]
    [SerializeField] private RectTransform joystickBase;
    [SerializeField] private RectTransform joystickKnob;
    [SerializeField] private float         joystickRadius = 60f;

    // ── Look pad ──────────────────────────────────────────────────────────
    [Header("Look Pad")]
    [SerializeField] private RectTransform lookPadRect;
    [SerializeField] private float         lookSensitivity = 0.15f;

    // ── Buttons ───────────────────────────────────────────────────────────
    [Header("Buttons")]
    [SerializeField] private Button fireButton;
    [SerializeField] private Button reloadButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button crouchButton;
    [SerializeField] private Button aimButton;
    [SerializeField] private Button medkitButton;
    [SerializeField] private Button weaponNextButton;
    [SerializeField] private Button weaponPrevButton;

    // ── Private ───────────────────────────────────────────────────────────
    private NetworkPlayerController _player;
    private int    _joystickTouchId = -1;
    private int    _lookTouchId     = -1;
    private Vector2 _lastLookPos;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        WireButtons();
        HideJoystick();
    }

    private void Update()
    {
        // Find local player lazily
        if (_player == null)
        {
            foreach (var p in FindObjectsOfType<NetworkPlayerController>())
                if (p.IsOwner) { _player = p; break; }
        }

        HandleTouchLook();
    }

    // ── Button wiring ─────────────────────────────────────────────────────
    private void WireButtons()
    {
        if (fireButton   != null) fireButton.onClick.AddListener(OnFirePressed);
        if (reloadButton != null) reloadButton.onClick.AddListener(OnReloadPressed);
        if (jumpButton   != null) jumpButton.onClick.AddListener(OnJumpPressed);
        if (crouchButton != null) crouchButton.onClick.AddListener(OnCrouchPressed);
        if (medkitButton != null) medkitButton.onClick.AddListener(OnMedkitPressed);
        if (weaponNextButton != null) weaponNextButton.onClick.AddListener(() => _player?.GetComponent<NetworkWeapon>()?.SwitchWeapon(1));
        if (weaponPrevButton != null) weaponPrevButton.onClick.AddListener(() => _player?.GetComponent<NetworkWeapon>()?.SwitchWeapon(-1));

        // Aim hold — use EventTrigger or PointerDownHandler on the aimButton parent
    }

    // ── Joystick touch ────────────────────────────────────────────────────
    public void OnPointerDown(PointerEventData e)
    {
        if (joystickBase == null) return;
        if (RectTransformUtility.RectangleContainsScreenPoint(joystickBase, e.position, e.pressEventCamera))
        {
            _joystickTouchId = e.pointerId;
            ShowJoystick(e.position);
        }
        else if (lookPadRect != null &&
                 RectTransformUtility.RectangleContainsScreenPoint(lookPadRect, e.position, e.pressEventCamera))
        {
            _lookTouchId = e.pointerId;
            _lastLookPos = e.position;
        }
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId == _joystickTouchId) UpdateJoystick(e.position);
        else if (e.pointerId == _lookTouchId) UpdateLook(e.position);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId == _joystickTouchId) { _joystickTouchId = -1; ResetJoystick(); }
        if (e.pointerId == _lookTouchId)     { _lookTouchId     = -1; }
    }

    private void ShowJoystick(Vector2 screenPos)
    {
        if (joystickBase == null) return;
        joystickBase.gameObject.SetActive(true);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBase.parent as RectTransform, screenPos, null, out Vector2 local);
        joystickBase.anchoredPosition = local;
    }

    private void HideJoystick()
    {
        if (joystickBase != null) joystickBase.gameObject.SetActive(false);
    }

    private void UpdateJoystick(Vector2 screenPos)
    {
        if (joystickBase == null || joystickKnob == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBase, screenPos, null, out Vector2 local);
        Vector2 clamped = Vector2.ClampMagnitude(local, joystickRadius);
        joystickKnob.anchoredPosition = clamped;

        Vector2 input = clamped / joystickRadius;
        _player?.SetMobileMove(input);
    }

    private void ResetJoystick()
    {
        if (joystickKnob != null) joystickKnob.anchoredPosition = Vector2.zero;
        HideJoystick();
        _player?.SetMobileMove(Vector2.zero);
    }

    // ── Look pad ──────────────────────────────────────────────────────────
    private void HandleTouchLook()
    {
        // Handled via OnDrag; kept here for multi-touch fallback if needed
    }

    private void UpdateLook(Vector2 screenPos)
    {
        Vector2 delta = (screenPos - _lastLookPos) * lookSensitivity;
        _lastLookPos  = screenPos;
        _player?.SetMobileLookDelta(delta);
    }

    // ── Button callbacks ──────────────────────────────────────────────────
    private void OnFirePressed()   => _player?.FireMobile();
    private void OnReloadPressed() => _player?.GetComponent<NetworkWeapon>()?.TryReload();
    private void OnJumpPressed()   => _player?.MobileJump();
    private void OnCrouchPressed() => _player?.MobileCrouch();
    private void OnMedkitPressed() => _player?.GetComponent<PlayerInventory>()?.UseMedkit();
}
