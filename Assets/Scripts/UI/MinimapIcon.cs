using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Attached to each NetworkPlayer. Maintains a small quad above the player
/// visible only to the minimap camera (Minimap layer).  The quad's colour
/// identifies the player: green = local owner, red = enemy.
///
/// The Scene Generator adds this automatically to the NetworkPlayer prefab.
/// </summary>
public class MinimapIcon : NetworkBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Icon appearance")]
    [SerializeField] private float iconSize        = 4f;   // world units (seen from above)
    [SerializeField] private float iconHeightOffset = 2f;  // above player pivot
    [SerializeField] private Color ownerColor      = Color.green;
    [SerializeField] private Color enemyColor      = Color.red;
    [SerializeField] private Color teamColor       = new Color(0.2f, 0.6f, 1f); // future teams

    [Header("Direction arrow")]
    [SerializeField] private bool showDirectionArrow = true;

    // ── Runtime ───────────────────────────────────────────────────────────
    private GameObject _iconQuad;
    private GameObject _arrowQuad;
    private Renderer   _iconRenderer;
    private Renderer   _arrowRenderer;

    // Layer name for the minimap camera to see
    private const string MinimapLayer = "UI";   // reuse built-in UI layer

    // ─────────────────────────────────────────────────────────────────────
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

        // Keep icon floating above player in world space
        _iconQuad.transform.position = transform.position + Vector3.up * iconHeightOffset;
        _iconQuad.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // flat, facing up

        if (_arrowQuad != null)
        {
            _arrowQuad.transform.position = _iconQuad.transform.position + Vector3.up * 0.1f;
            _arrowQuad.transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
        }
    }

    // ── Build icon ────────────────────────────────────────────────────────
    private void CreateIcon()
    {
        // ── Main dot ─────────────────────────────────────────────────────
        _iconQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _iconQuad.name = "MinimapDot_" + OwnerClientId;
        _iconQuad.transform.localScale = Vector3.one * iconSize;

        // Remove collider
        var col = _iconQuad.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Set layer
        int layer = LayerMask.NameToLayer(MinimapLayer);
        if (layer >= 0) _iconQuad.layer = layer;

        // Material
        _iconRenderer = _iconQuad.GetComponent<Renderer>();
        _iconRenderer.material = CreateFlatMaterial(IsOwner ? ownerColor : enemyColor);

        // ── Direction arrow ───────────────────────────────────────────────
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
            // Fallback to Standard if Unlit/Color not available
            mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color);
        }
        mat.color = color;
        return mat;
    }

    // ── Colour change (for future team system) ────────────────────────────
    public void SetIconColor(Color c)
    {
        if (_iconRenderer != null) _iconRenderer.material.color = c;
    }
}
