using UnityEngine;

/// <summary>
/// Attached to the muzzle point Transform on a weapon model.
/// NetworkWeapon calls Flash() on the owner client whenever a shot fires.
/// A light pulse is included; assign a point light child or leave unassigned
/// for the procedural fallback from VFXManager.
/// </summary>
public class MuzzleFlash : MonoBehaviour
{
    [Header("Light flash")]
    [SerializeField] private Light muzzleLight;
    [SerializeField] private float flashIntensity  = 6f;
    [SerializeField] private float flashRange      = 4f;
    [SerializeField] private float flashDuration   = 0.06f;
    [SerializeField] private Color flashColor      = new Color(1f, 0.9f, 0.5f);

    [Header("Shell eject (optional)")]
    [SerializeField] private ParticleSystem shellEjectParticles;

    private float _flashTimer;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Auto-create a child light if none assigned
        if (muzzleLight == null)
        {
            var lightGo = new GameObject("MuzzleLight");
            lightGo.transform.SetParent(transform);
            lightGo.transform.localPosition = Vector3.zero;
            muzzleLight = lightGo.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
        }

        muzzleLight.enabled   = false;
        muzzleLight.color     = flashColor;
        muzzleLight.intensity = flashIntensity;
        muzzleLight.range     = flashRange;
    }

    private void Update()
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
                muzzleLight.enabled = false;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Call from NetworkWeapon on the owning client to show a muzzle flash.
    /// </summary>
    public void Flash()
    {
        // Particle VFX
        VFXManager.Instance?.PlayMuzzleFlash(transform.position, transform.rotation);

        // Light
        if (muzzleLight != null)
        {
            muzzleLight.enabled   = true;
            muzzleLight.intensity = flashIntensity * Random.Range(0.8f, 1.2f);
            _flashTimer = flashDuration;
        }

        // Shell eject
        if (shellEjectParticles != null)
        {
            shellEjectParticles.Play();
        }
    }

    /// <summary>
    /// Returns the forward direction of the muzzle (used by weapon for spawn position).
    /// </summary>
    public Vector3 MuzzleForward => transform.forward;
    public Vector3 MuzzlePosition => transform.position;
}
