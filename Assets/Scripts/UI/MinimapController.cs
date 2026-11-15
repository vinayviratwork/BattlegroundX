using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the minimap: owns the overhead render-texture camera, keeps it
/// above the local player, and displays the result in a RawImage on the HUD.
///
/// Setup (done automatically by the Scene Generator):
///   1. A Camera component (MinimapCamera) is parented to this GameObject.
///   2. A RenderTexture (256×256) is created and assigned to both the camera
///      and the RawImage in the HUD.
///   3. Each spawned NetworkPlayer receives a MinimapIcon component that
///      registers itself here so the minimap dot moves with the player.
///
/// No manual setup needed if you use the Scene Generator.
/// </summary>
public class MinimapController : MonoBehaviour
{
    public static MinimapController Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Camera")]
    [SerializeField] private Camera   minimapCamera;
    [SerializeField] private float    cameraHeight    = 120f;
    [SerializeField] private float    orthographicSize = 90f; // half the view in units

    [Header("Render Texture")]
    [SerializeField] private RawImage hudMinimapImage;    // wire in Inspector / scene gen
    [SerializeField] private int      rtWidth  = 256;
    [SerializeField] private int      rtHeight = 256;

    [Header("Minimap border")]
    [SerializeField] private bool  circularMask = true;
    [SerializeField] private Image maskImage;             // optional circle mask image

    [Header("Rotation")]
    [SerializeField] private bool  rotateWithPlayer = true; // map rotates to face north or player

    // ── Runtime ───────────────────────────────────────────────────────────
    private RenderTexture _rt;
    private Transform     _localPlayerTransform;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        SetupRenderTexture();
        SetupCamera();
    }

    private void LateUpdate()
    {
        FollowPlayer();
        if (rotateWithPlayer) RotateWithPlayer();
    }

    // ── Setup ─────────────────────────────────────────────────────────────
    private void SetupRenderTexture()
    {
        _rt = new RenderTexture(rtWidth, rtHeight, 16, RenderTextureFormat.ARGB32);
        _rt.name = "MinimapRT";
        _rt.Create();

        if (hudMinimapImage != null)
            hudMinimapImage.texture = _rt;
    }

    private void SetupCamera()
    {
        if (minimapCamera == null)
        {
            var camGo = new GameObject("MinimapCamera");
            camGo.transform.SetParent(transform);
            minimapCamera = camGo.AddComponent<Camera>();
        }

        minimapCamera.orthographic     = true;
        minimapCamera.orthographicSize = orthographicSize;
        minimapCamera.cullingMask      = GetMinimapLayerMask();
        minimapCamera.clearFlags       = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor  = new Color(0.1f, 0.15f, 0.1f, 1f);
        minimapCamera.nearClipPlane    = 1f;
        minimapCamera.farClipPlane     = cameraHeight + 10f;
        minimapCamera.targetTexture    = _rt;
        minimapCamera.depth            = -2; // render before main camera

        // Face straight down
        minimapCamera.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static int GetMinimapLayerMask()
    {
        // Include Default + UI layer (MinimapIcon quads are on UI layer)
        int mask = LayerMask.GetMask("Default", "UI");
        return mask == 0 ? ~0 : mask;
    }

    // ── Follow ────────────────────────────────────────────────────────────
    private void FollowPlayer()
    {
        if (_localPlayerTransform == null)
        {
            FindLocalPlayer();
            if (_localPlayerTransform == null) return;
        }

        Vector3 playerPos = _localPlayerTransform.position;
        minimapCamera.transform.position = new Vector3(playerPos.x, playerPos.y + cameraHeight, playerPos.z);
    }

    private void RotateWithPlayer()
    {
        if (_localPlayerTransform == null) return;
        float yaw = _localPlayerTransform.eulerAngles.y;
        minimapCamera.transform.localRotation = Quaternion.Euler(90f, yaw, 0f);
    }

    private void FindLocalPlayer()
    {
        foreach (var ctrl in FindObjectsOfType<NetworkPlayerController>())
        {
            if (ctrl.IsOwner)
            {
                _localPlayerTransform = ctrl.transform;
                break;
            }
        }
    }

    // ── Public API: icons register/unregister themselves ──────────────────
    public void RegisterIcon(MinimapIcon icon)   { /* icons self-manage; hook for future list UI */ }
    public void UnregisterIcon(MinimapIcon icon) { }

    // ── Zoom ──────────────────────────────────────────────────────────────
    public void SetZoom(float worldUnitsHalfSize)
    {
        orthographicSize = Mathf.Clamp(worldUnitsHalfSize, 20f, 180f);
        if (minimapCamera != null)
            minimapCamera.orthographicSize = orthographicSize;
    }

    private void OnDestroy()
    {
        if (_rt != null) _rt.Release();
    }
}
