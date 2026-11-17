using UnityEngine;

public class MinimapSafeZoneRing : MonoBehaviour
{
    [SerializeField] private Color ringColor      = new Color(0.2f, 0.6f, 1f, 0.7f);
    [SerializeField] private float ringThickness  = 1.5f; 

    private GameObject     _ringGo;
    private Renderer       _ringRenderer;
    private NetworkSafeZone _zone;

    private const string MinimapLayer = "UI";

    
    private void Start()
    {
        _zone = FindObjectOfType<NetworkSafeZone>();
        CreateRing();
    }

    private void LateUpdate()
    {
        if (_zone == null || _ringGo == null) return;

        float radius = _zone.CurrentRadius;
        
        _ringGo.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);

        
        _ringGo.transform.position = new Vector3(
            _zone.CurrentCenter.x,
            0.5f,
            _zone.CurrentCenter.z);
    }

    
    private void CreateRing()
    {
        
        _ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _ringGo.name = "MinimapZoneRing";

        var col = _ringGo.GetComponent<Collider>();
        if (col != null) Destroy(col);

        int layer = LayerMask.NameToLayer(MinimapLayer);
        if (layer >= 0) _ringGo.layer = layer;

        _ringRenderer = _ringGo.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Standard"));
        mat.color = ringColor;
        
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;
        _ringRenderer.material = mat;
    }
}