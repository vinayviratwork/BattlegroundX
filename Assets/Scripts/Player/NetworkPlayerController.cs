using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Third-person networked player controller.
/// Supports: walk, run, sprint, crouch, jump, aim, camera follow/rotate.
/// PC: WASD + mouse. Mobile: virtual joystick bridge.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class NetworkPlayerController : NetworkBehaviour
{
    // ── Movement ──────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float walkSpeed    = 4.5f;
    [SerializeField] private float runSpeed     = 7f;
    [SerializeField] private float sprintSpeed  = 10f;
    [SerializeField] private float crouchSpeed  = 2.5f;
    [SerializeField] private float jumpForce    = 7f;
    [SerializeField] private float gravity      = -20f;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundMask;

    // ── Camera ────────────────────────────────────────────────────────────
    [Header("Camera")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera    playerCamera;
    [SerializeField] private float     mouseSensitivity  = 2f;
    [SerializeField] private float     aimSensitivityMult = 0.45f;
    [SerializeField] private float     minPitch = -60f;
    [SerializeField] private float     maxPitch =  60f;

    // ── Camera offset positions ───────────────────────────────────────────
    [Header("Camera Offset")]
    [SerializeField] private Vector3 normalCameraOffset = new Vector3(0.4f, 0.2f, -3.5f);
    [SerializeField] private Vector3 aimCameraOffset    = new Vector3(0.3f, 0.1f, -1.8f);
    [SerializeField] private float   cameraLerpSpeed    = 10f;

    // ── Crouch ────────────────────────────────────────────────────────────
    [Header("Crouch")]
    [SerializeField] private float normalHeight  = 2f;
    [SerializeField] private float crouchHeight  = 1.1f;
    [SerializeField] private float crouchCenterY = 0.55f;
    [SerializeField] private float normalCenterY = 1f;

    // ── State (networked) ─────────────────────────────────────────────────
    public NetworkVariable<bool>  IsRunning  = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool>  IsCrouching = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool>  IsAiming   = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector3> NetworkPosition = new(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // ── Private ───────────────────────────────────────────────────────────
    private CharacterController _cc;
    private float _verticalVelocity;
    private float _pitch;
    private bool  _isCrouching;
    private bool  _isAiming;
    private bool  _isGrounded;

    // Mobile input
    private Vector2 _mobileMove;
    private Vector2 _mobileLookDelta;

    // Camera follow transform for non-owner
    private Transform _cameraTransform;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        bool owner = IsOwner;
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(owner);
            _cameraTransform = playerCamera.transform;
        }
        if (cameraPivot != null) cameraPivot.gameObject.SetActive(owner);
        if (owner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }

    private void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        HandleGroundCheck();
        HandleCrouchInput();
        HandleMovement();
        HandleCameraRotation();
        HandleAim();
        HandleJump();
        HandleGravity();
        SyncCameraOffset();

        // Broadcast position for lag-compensation
        if (IsOwner) NetworkPosition.Value = transform.position;
    }

    // ── Ground ────────────────────────────────────────────────────────────
    private void HandleGroundCheck()
    {
        _isGrounded = Physics.CheckSphere(
            transform.position + Vector3.down * (_cc.height * 0.5f - 0.05f),
            groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    // ── Movement ──────────────────────────────────────────────────────────
    private void HandleMovement()
    {
        Vector2 input = GetMoveInput();

        bool sprinting = Input.GetKey(KeyCode.LeftShift) && !_isCrouching && input.sqrMagnitude > 0.1f;
        bool running   = input.sqrMagnitude > 0.1f && !sprinting;

        float speed = _isCrouching ? crouchSpeed :
                      sprinting    ? sprintSpeed  :
                      running      ? runSpeed      :
                                     walkSpeed;

        Vector3 dir = (transform.right * input.x + transform.forward * input.y).normalized;
        Vector3 velocity = dir * speed;
        velocity.y = _verticalVelocity;
        _cc.Move(velocity * Time.deltaTime);

        IsRunning.Value   = running || sprinting;
        IsAiming.Value    = _isAiming;
        IsCrouching.Value = _isCrouching;
    }

    private Vector2 GetMoveInput()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return _mobileMove;
#else
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        return Vector2.ClampMagnitude(new Vector2(h, v), 1f);
#endif
    }

    // ── Jump ──────────────────────────────────────────────────────────────
    private void HandleJump()
    {
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);
        if (jumpPressed && _isGrounded && !_isCrouching)
        {
            _verticalVelocity = jumpForce;
            _isCrouching = false;
            SetCrouchShape(false);
        }
    }

    // ── Gravity ───────────────────────────────────────────────────────────
    private void HandleGravity()
    {
        if (_isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;
        else
            _verticalVelocity += gravity * Time.deltaTime;
        _verticalVelocity = Mathf.Max(_verticalVelocity, gravity);
    }

    // ── Crouch ────────────────────────────────────────────────────────────
    private void HandleCrouchInput()
    {
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            _isCrouching = !_isCrouching;
            SetCrouchShape(_isCrouching);
        }
    }

    private void SetCrouchShape(bool crouching)
    {
        _cc.height = crouching ? crouchHeight : normalHeight;
        _cc.center = new Vector3(0, crouching ? crouchCenterY : normalCenterY, 0);
    }

    // ── Aim ───────────────────────────────────────────────────────────────
    private void HandleAim()
    {
        _isAiming = Input.GetMouseButton(1);
    }

    // ── Camera rotation ───────────────────────────────────────────────────
    private void HandleCameraRotation()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Vector2 look = _mobileLookDelta;
        _mobileLookDelta = Vector2.zero;
#else
        float sens = _isAiming ? mouseSensitivity * aimSensitivityMult : mouseSensitivity;
        Vector2 look = new Vector2(
            Input.GetAxis("Mouse X") * sens,
            Input.GetAxis("Mouse Y") * sens);
#endif
        ApplyLook(look);
    }

    private void ApplyLook(Vector2 delta)
    {
        transform.Rotate(Vector3.up * delta.x);
        _pitch = Mathf.Clamp(_pitch - delta.y, minPitch, maxPitch);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // ── Camera offset lerp (hip / aim) ────────────────────────────────────
    private void SyncCameraOffset()
    {
        if (_cameraTransform == null) return;
        Vector3 target = _isAiming ? aimCameraOffset : normalCameraOffset;
        _cameraTransform.localPosition = Vector3.Lerp(
            _cameraTransform.localPosition, target, cameraLerpSpeed * Time.deltaTime);
    }

    // ── Mobile API ────────────────────────────────────────────────────────
    public void SetMobileMove(Vector2 value) => _mobileMove = Vector2.ClampMagnitude(value, 1f);
    public void SetMobileLookDelta(Vector2 delta) { if (IsOwner) _mobileLookDelta += delta * mouseSensitivity; }
    public void MobileCrouch() { if (IsOwner) { _isCrouching = !_isCrouching; SetCrouchShape(_isCrouching); } }
    public void MobileJump()   { if (IsOwner && _isGrounded) _verticalVelocity = jumpForce; }
    public void MobileAimStart() { if (IsOwner) _isAiming = true; }
    public void MobileAimEnd()   { if (IsOwner) _isAiming = false; }
    public void FireMobile()   { if (IsOwner) GetComponent<NetworkWeapon>()?.TryFire(); }

    // ── Public getters ────────────────────────────────────────────────────
    public Camera PlayerCamera => playerCamera;
    public bool   LocalIsAiming => _isAiming;
}
