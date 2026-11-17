using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    public static MinimapController Instance { get; private set; }

    
    [Header("Camera")]
    [SerializeField] private Camera   minimapCamera;
    [SerializeField] private float    cameraHeight    = 120f;
    [SerializeField] private float    orthographicSize = 90f; 

    [Header("Render Texture")]
    [SerializeField] private RawImage hudMinimapImage;    
    [SerializeField] private int      rtWidth  = 256;
    [SerializeField] private int      rtHeight = 256;

    [Header("Minimap border")]
    [SerializeField] private bool  circularMask = true;
    [SerializeField] private Image maskImage;             

    [Header("Rotation")]
    [SerializeField] private bool  rotateWithPlayer = true; 

    
    private RenderTexture _rt;
    private Transform     _localPlayerTransform;

    
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
        minimapCamera.depth            = -2; 

        
        minimapCamera.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static int GetMinimapLayerMask()
    {
        
        int mask = LayerMask.GetMask("Default", "UI");
        return mask == 0 ? ~0 : mask;
    }

    
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

    
    public void RegisterIcon(MinimapIcon icon)   {  }
    public void UnregisterIcon(MinimapIcon icon) { }

    
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