using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    
    [Header("Prefabs (optional — procedural fallback if unassigned)")]
    [SerializeField] private ParticleSystem muzzleFlashPrefab;
    [SerializeField] private ParticleSystem hitSparkPrefab;
    [SerializeField] private ParticleSystem bloodPrefab;
    [SerializeField] private ParticleSystem explosionPrefab;

    [Header("Pool sizes")]
    [SerializeField] private int muzzlePoolSize    = 8;
    [SerializeField] private int hitPoolSize       = 16;
    [SerializeField] private int bloodPoolSize     = 16;
    [SerializeField] private int explosionPoolSize = 4;

    
    private Queue<ParticleSystem> _muzzlePool    = new();
    private Queue<ParticleSystem> _hitPool       = new();
    private Queue<ParticleSystem> _bloodPool     = new();
    private Queue<ParticleSystem> _explosionPool = new();

    
    [Header("Screen flash")]
    [SerializeField] private UnityEngine.UI.Image screenFlashImage;
    [SerializeField] private float                screenFlashDuration = 0.1f;
    [SerializeField] private Color                damageFlashColor    = new Color(1f, 0f, 0f, 0.35f);
    [SerializeField] private Color                healFlashColor      = new Color(0f, 1f, 0f, 0.25f);

    
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        BuildPool(ref _muzzlePool,    muzzleFlashPrefab,  muzzlePoolSize,    CreateMuzzleSystem);
        BuildPool(ref _hitPool,       hitSparkPrefab,     hitPoolSize,       CreateHitSparkSystem);
        BuildPool(ref _bloodPool,     bloodPrefab,        bloodPoolSize,     CreateBloodSystem);
        BuildPool(ref _explosionPool, explosionPrefab,    explosionPoolSize, CreateExplosionSystem);
    }

    
    private void BuildPool(ref Queue<ParticleSystem> pool, ParticleSystem prefab,
                           int size, System.Func<ParticleSystem> factory)
    {
        for (int i = 0; i < size; i++)
        {
            ParticleSystem ps = prefab != null
                ? Instantiate(prefab, transform)
                : factory();
            ps.gameObject.SetActive(false);
            pool.Enqueue(ps);
        }
    }

    
    public void PlayMuzzleFlash(Vector3 position, Quaternion rotation)
        => Play(_muzzlePool, position, rotation, 0.15f);

    public void PlayHitEffect(Vector3 position, Vector3 normal)
        => Play(_hitPool, position, Quaternion.LookRotation(normal), 0.4f);

    public void PlayBloodEffect(Vector3 position, Vector3 normal)
        => Play(_bloodPool, position, Quaternion.LookRotation(normal), 0.6f);

    public void PlayExplosion(Vector3 position)
        => Play(_explosionPool, position, Quaternion.identity, 1.5f);

    public void FlashDamage()  => StartCoroutine(DoScreenFlash(damageFlashColor));
    public void FlashHeal()    => StartCoroutine(DoScreenFlash(healFlashColor));

    
    private void Play(Queue<ParticleSystem> pool, Vector3 pos, Quaternion rot, float returnDelay)
    {
        if (pool.Count == 0) return;
        var ps = pool.Dequeue();
        ps.transform.SetPositionAndRotation(pos, rot);
        ps.gameObject.SetActive(true);
        ps.Play();
        StartCoroutine(ReturnToPool(ps, pool, returnDelay));
    }

    private IEnumerator ReturnToPool(ParticleSystem ps, Queue<ParticleSystem> pool, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps == null) yield break;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);
        pool.Enqueue(ps);
    }

    
    private IEnumerator DoScreenFlash(Color color)
    {
        if (screenFlashImage == null) yield break;
        screenFlashImage.color = color;
        screenFlashImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(screenFlashDuration);
        screenFlashImage.gameObject.SetActive(false);
    }

    

    private ParticleSystem CreateMuzzleSystem()
    {
        var go = new GameObject("VFX_MuzzleFlash");
        go.transform.SetParent(transform);
        var ps = go.AddComponent<ParticleSystem>();

        var main      = ps.main;
        main.duration        = 0.1f;
        main.loop            = false;
        main.startLifetime   = 0.1f;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.3f), new Color(1f, 0.5f, 0.1f));
        main.maxParticles    = 30;

        var emission  = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

        var shape     = ps.shape;
        shape.enabled     = true;
        shape.shapeType   = ParticleSystemShapeType.Cone;
        shape.angle       = 25f;
        shape.radius      = 0.02f;

        var lights    = ps.lights;
        lights.enabled         = true;
        lights.ratio           = 1f;
        lights.intensityMultiplier = 3f;
        lights.rangeMultiplier     = 1f;
        lights.maxLights           = 1;

        return ps;
    }

    private ParticleSystem CreateHitSparkSystem()
    {
        var go = new GameObject("VFX_HitSpark");
        go.transform.SetParent(transform);
        var ps = go.AddComponent<ParticleSystem>();

        var main   = ps.main;
        main.duration      = 0.3f;
        main.loop          = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
        main.startColor    = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.8f, 0.2f), new Color(1f, 0.4f, 0.1f));
        main.gravityModifier = 0.5f;
        main.maxParticles  = 20;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius    = 0.05f;

        return ps;
    }

    private ParticleSystem CreateBloodSystem()
    {
        var go = new GameObject("VFX_Blood");
        go.transform.SetParent(transform);
        var ps = go.AddComponent<ParticleSystem>();

        var main   = ps.main;
        main.duration      = 0.5f;
        main.loop          = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(1f, 4f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor    = new ParticleSystem.MinMaxGradient(
            new Color(0.6f, 0f, 0f), new Color(0.9f, 0.1f, 0.1f));
        main.gravityModifier = 1.2f;
        main.maxParticles  = 25;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius    = 0.08f;

        return ps;
    }

    private ParticleSystem CreateExplosionSystem()
    {
        var go = new GameObject("VFX_Explosion");
        go.transform.SetParent(transform);
        var ps = go.AddComponent<ParticleSystem>();

        var main   = ps.main;
        main.duration        = 1f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(5f, 15f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.3f, 1f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.6f, 0.1f), new Color(1f, 0.2f, 0f));
        main.gravityModifier = -0.3f;
        main.maxParticles    = 80;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.3f;

        var trails = ps.trails;
        trails.enabled = true;
        trails.ratio   = 0.3f;
        trails.lifetime = 0.3f;
        trails.minVertexDistance = 0.05f;

        return ps;
    }
}