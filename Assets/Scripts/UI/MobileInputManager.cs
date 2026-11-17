using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileInputManager : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public static MobileInputManager Instance { get; private set; }

    
    [Header("Joystick")]
    [SerializeField] private RectTransform joystickBase;
    [SerializeField] private RectTransform joystickKnob;
    [SerializeField] private float         joystickRadius = 60f;

    
    [Header("Look Pad")]
    [SerializeField] private RectTransform lookPadRect;
    [SerializeField] private float         lookSensitivity = 0.15f;

    
    [Header("Buttons")]
    [SerializeField] private Button fireButton;
    [SerializeField] private Button reloadButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button crouchButton;
    [SerializeField] private Button aimButton;
    [SerializeField] private Button medkitButton;
    [SerializeField] private Button weaponNextButton;
    [SerializeField] private Button weaponPrevButton;

    
    private NetworkPlayerController _player;
    private int    _joystickTouchId = -1;
    private int    _lookTouchId     = -1;
    private Vector2 _lastLookPos;

    
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
        
        if (_player == null)
        {
            foreach (var p in FindObjectsOfType<NetworkPlayerController>())
                if (p.IsOwner) { _player = p; break; }
        }

        HandleTouchLook();
    }

    
    private void WireButtons()
    {
        if (fireButton   != null) fireButton.onClick.AddListener(OnFirePressed);
        if (reloadButton != null) reloadButton.onClick.AddListener(OnReloadPressed);
        if (jumpButton   != null) jumpButton.onClick.AddListener(OnJumpPressed);
        if (crouchButton != null) crouchButton.onClick.AddListener(OnCrouchPressed);
        if (medkitButton != null) medkitButton.onClick.AddListener(OnMedkitPressed);
        if (weaponNextButton != null) weaponNextButton.onClick.AddListener(() => _player?.GetComponent<NetworkWeapon>()?.SwitchWeapon(1));
        if (weaponPrevButton != null) weaponPrevButton.onClick.AddListener(() => _player?.GetComponent<NetworkWeapon>()?.SwitchWeapon(-1));

        
    }

    
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

    
    private void HandleTouchLook()
    {
        
    }

    private void UpdateLook(Vector2 screenPos)
    {
        Vector2 delta = (screenPos - _lastLookPos) * lookSensitivity;
        _lastLookPos  = screenPos;
        _player?.SetMobileLookDelta(delta);
    }

    
    private void OnFirePressed()   => _player?.FireMobile();
    private void OnReloadPressed() => _player?.GetComponent<NetworkWeapon>()?.TryReload();
    private void OnJumpPressed()   => _player?.MobileJump();
    private void OnCrouchPressed() => _player?.MobileCrouch();
    private void OnMedkitPressed() => _player?.GetComponent<PlayerInventory>()?.UseMedkit();
}