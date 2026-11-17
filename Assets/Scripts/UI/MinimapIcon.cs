using Unity.Netcode;
using UnityEngine;

public class MinimapIcon : NetworkBehaviour
{
    
    [Header("Icon appearance")]
    [SerializeField] private float iconSize        = 4f;   
    [SerializeField] private float iconHeightOffset = 2f;  
    [SerializeField] private Color ownerColor      = Color.green;
    [SerializeField] private Color enemyColor      = Color.red;
    [SerializeField] private Color teamColor       = new Color(0.2f, 0.6f, 1f); 

    [Header("Direction arrow")]
    [SerializeField] private bool showDirectionArrow = true;

    
    private GameObject _iconQuad;
    private GameObject _arrowQuad;
    private Renderer   _iconRenderer;
    private Renderer   _arrowRenderer;

    
    private const string MinimapLayer = "UI";   

    
    public override void OnNetworkSpawn()
    {
        CreateIcon();
        MinimapController.Instance?.RegisterIcon(this);
    }

    public override void OnNetworkDespawn()
    {
        MinimapController.Instance?.UnregisterIcon(this);
        if (_iconQuad  != null) Destroy(_iconQuad);
        if (_arrowQuad != null) Destroy(_arrowQuad);
    }

    private void LateUpdate()
    {
        if (_iconQuad == null) return;

        
        _iconQuad.transform.position = transform.position + Vector3.up * iconHeightOffset;
        _iconQuad.transform.rotation = Quaternion.Euler(90f, 0f, 0f); 

        if (_arrowQuad != null)
        {
            _arrowQuad.transform.position = _iconQuad.transform.position + Vector3.up * 0.1f;
            _arrowQuad.transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
        }
    }

    
    private void CreateIcon()
    {
        
        _iconQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _iconQuad.name = "MinimapDot_" + OwnerClientId;
        _iconQuad.transform.localScale = Vector3.one * iconSize;

        
        var col = _iconQuad.GetComponent<Collider>();
        if (col != null) Destroy(col);

        
        int layer = LayerMask.NameToLayer(MinimapLayer);
        if (layer >= 0) _iconQuad.layer = layer;

        
        _iconRenderer = _iconQuad.GetComponent<Renderer>();
        _iconRenderer.material = CreateFlatMaterial(IsOwner ? ownerColor : enemyColor);

        
        if (showDirectionArrow)
        {
            _arrowQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _arrowQuad.name = "MinimapArrow_" + OwnerClientId;
            _arrowQuad.transform.localScale = new Vector3(iconSize * 0.4f, iconSize * 0.8f, 1f);

            var arrowCol = _arrowQuad.GetComponent<Collider>();
            if (arrowCol != null) Destroy(arrowCol);

            if (layer >= 0) _arrowQuad.layer = layer;
            _arrowRenderer = _arrowQuad.GetComponent<Renderer>();
            _arrowRenderer.material = CreateFlatMaterial(IsOwner
                ? new Color(ownerColor.r, ownerColor.g, ownerColor.b, 0.75f)
                : new Color(enemyColor.r, enemyColor.g, enemyColor.b, 0.75f));
        }
    }

    private static Material CreateFlatMaterial(Color color)
    {
        var mat = new Material(Shader.Find("Unlit/Color"));
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
        {
            
            mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color);
        }
        mat.color = color;
        return mat;
    }

    
    public void SetIconColor(Color c)
    {
        if (_iconRenderer != null) _iconRenderer.material.color = c;
    }
}