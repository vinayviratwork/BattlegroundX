using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// World-space name tag that floats above each player.
/// Shows "Player {clientId}" by default; can be updated with a real name via
/// SetDisplayName() once lobby/auth name retrieval is implemented.
///
/// Faces the local player's camera each frame (billboarding).
/// Automatically hidden when:
///   - The player is dead (IsDead = true)
///   - The distance exceeds maxVisibleDistance
///   - This is the local owner (you don't see your own tag)
/// </summary>
public class NameTag : NetworkBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Tag settings")]
    [SerializeField] private float heightAbovePlayer  = 2.4f;
    [SerializeField] private float maxVisibleDistance = 60f;
    [SerializeField] private float tagScale           = 0.015f;

    [Header("Colours")]
    [SerializeField] private Color aliveColor   = Color.white;
    [SerializeField] private Color lowHPColor   = Color.red;
    [SerializeField] private int   lowHPThreshold = 30;

    // ── Networked name ────────────────────────────────────────────────────
    public NetworkVariable<Unity.Collections.FixedString64Bytes> DisplayName =
        new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // ── Runtime ───────────────────────────────────────────────────────────
    private GameObject  _tagRoot;
    private TMP_Text    _nameText;
    private TMP_Text    _hpText;
    private NetworkHealth _health;
    private Camera      _localCamera;

    // ─────────────────────────────────────────────────────────────────────
    public override void OnNetworkSpawn()
    {
        _health = GetComponent<NetworkHealth>();

        // Owner: set default name and hide their own tag
        if (IsOwner)
        {
            DisplayName.Value = new Unity.Collections.FixedString64Bytes(
                $"Player {OwnerClientId}");
        }

        // All clients build the tag object (we hide it on owner)
        BuildTagObject();
        DisplayName.OnValueChanged += OnNameChanged;
        RefreshNameText();

        // Hide own tag
        if (IsOwner && _tagRoot != null)
            _tagRoot.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        DisplayName.OnValueChanged -= OnNameChanged;
        if (_tagRoot != null) Destroy(_tagRoot);
    }

    private void LateUpdate()
    {
        if (_tagRoot == null || IsOwner) return;

        UpdateVisibility();
        BillboardToCamera();
        UpdateHealthColour();
    }

    // ── Visibility ────────────────────────────────────────────────────────
    private void UpdateVisibility()
    {
        bool dead = _health != null && _health.IsDead.Value;
        if (dead)
        {
            _tagRoot.SetActive(false);
            return;
        }

        // Distance check
        Camera cam = GetLocalCamera();
        if (cam == null) return;

        float dist = Vector3.Distance(transform.position, cam.transform.position);
        _tagRoot.SetActive(dist <= maxVisibleDistance);

        // Keep position above head
        _tagRoot.transform.position = transform.position + Vector3.up * heightAbovePlayer;
    }

    // ── Billboard ─────────────────────────────────────────────────────────
    private void BillboardToCamera()
    {
        Camera cam = GetLocalCamera();
        if (cam == null || _tagRoot == null) return;

        Vector3 dir = _tagRoot.transform.position - cam.transform.position;
        if (dir.sqrMagnitude > 0.001f)
            _tagRoot.transform.rotation = Quaternion.LookRotation(dir);
    }

    // ── HP colour ─────────────────────────────────────────────────────────
    private void UpdateHealthColour()
    {
        if (_health == null || _nameText == null) return;

        int hp = _health.CurrentHealth.Value;
        _nameText.color = hp <= lowHPThreshold ? lowHPColor : aliveColor;
        if (_hpText != null)
            _hpText.text = $"♥ {hp}";
    }

    // ── Build world-space UI ──────────────────────────────────────────────
    private void BuildTagObject()
    {
        _tagRoot = new GameObject("NameTag_" + OwnerClientId);
        _tagRoot.transform.SetParent(null); // world space

        // Name label
        var nameGo  = new GameObject("NameText");
        nameGo.transform.SetParent(_tagRoot.transform);
        nameGo.transform.localPosition = Vector3.zero;
        nameGo.transform.localScale    = Vector3.one * tagScale;

        _nameText      = nameGo.AddComponent<TMP_Text>();  // actually TextMeshPro
        // We use TextMeshPro in world space
        // Replace TMP_Text with TextMeshPro for world-space
        Destroy(_nameText);
        var tmPro = nameGo.AddComponent<TextMeshPro>();
        _nameText = tmPro;
        tmPro.alignment       = TextAlignmentOptions.Center;
        tmPro.fontSize        = 3f;
        tmPro.fontStyle       = FontStyles.Bold;
        tmPro.color           = aliveColor;
        tmPro.outlineWidth    = 0.2f;
        tmPro.outlineColor    = Color.black;

        // HP label (smaller, below name)
        var hpGo = new GameObject("HPText");
        hpGo.transform.SetParent(_tagRoot.transform);
        hpGo.transform.localPosition = new Vector3(0f, -0.3f / tagScale * tagScale, 0f);
        hpGo.transform.localScale    = Vector3.one * tagScale * 0.8f;

        var hpTmp = hpGo.AddComponent<TextMeshPro>();
        _hpText = hpTmp;
        hpTmp.alignment  = TextAlignmentOptions.Center;
        hpTmp.fontSize   = 2.5f;
        hpTmp.color      = aliveColor;
        hpTmp.outlineWidth = 0.15f;
        hpTmp.outlineColor = Color.black;

        RefreshNameText();
    }

    // ── Name change callback ──────────────────────────────────────────────
    private void OnNameChanged(
        Unity.Collections.FixedString64Bytes oldVal,
        Unity.Collections.FixedString64Bytes newVal)
    {
        RefreshNameText();
    }

    private void RefreshNameText()
    {
        if (_nameText == null) return;
        _nameText.text = DisplayName.Value.ToString();
    }

    // ── Camera helper ─────────────────────────────────────────────────────
    private Camera GetLocalCamera()
    {
        if (_localCamera != null) return _localCamera;
        _localCamera = Camera.main;
        return _localCamera;
    }

    // ── Public API ────────────────────────────────────────────────────────
    public void SetDisplayName(string name)
    {
        if (IsOwner)
            DisplayName.Value = new Unity.Collections.FixedString64Bytes(name);
    }
}
