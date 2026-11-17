using UnityEngine;

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

    
    private void Awake()
    {
        
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

    public void Flash()
    {
        
        VFXManager.Instance?.PlayMuzzleFlash(transform.position, transform.rotation);

        
        if (muzzleLight != null)
        {
            muzzleLight.enabled   = true;
            muzzleLight.intensity = flashIntensity * Random.Range(0.8f, 1.2f);
            _flashTimer = flashDuration;
        }

        
        if (shellEjectParticles != null)
        {
            shellEjectParticles.Play();
        }
    }

    public Vector3 MuzzleForward => transform.forward;
    public Vector3 MuzzlePosition => transform.position;
}