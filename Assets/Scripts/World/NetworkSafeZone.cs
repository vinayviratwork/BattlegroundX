using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Multi-phase shrinking safe zone. Server-authoritative radius synced to all clients.
/// Deals escalating damage to players outside the zone each tick.
/// </summary>
public class NetworkSafeZone : NetworkBehaviour
{
    // ── Phase definition ──────────────────────────────────────────────────
    [System.Serializable]
    private class ZonePhase
    {
        public float targetRadius  = 100f;
        public float shrinkTime    = 120f;
        public float holdTime      = 30f;
        public float damagePerTick = 5f;
        public float tickInterval  = 1f;
    }

    [Header("Zone Phases")]
    [SerializeField] private List<ZonePhase> phases = new()
    {
        new ZonePhase { targetRadius=160f, shrinkTime=0f,   holdTime=30f, damagePerTick=0,   tickInterval=1f },
        new ZonePhase { targetRadius=120f, shrinkTime=90f,  holdTime=20f, damagePerTick=5,   tickInterval=1f },
        new ZonePhase { targetRadius= 80f, shrinkTime=90f,  holdTime=20f, damagePerTick=8,   tickInterval=1f },
        new ZonePhase { targetRadius= 50f, shrinkTime=80f,  holdTime=15f, damagePerTick=12,  tickInterval=1f },
        new ZonePhase { targetRadius= 25f, shrinkTime=70f,  holdTime=15f, damagePerTick=18,  tickInterval=1f },
        new ZonePhase { targetRadius= 10f, shrinkTime=60f,  holdTime=10f, damagePerTick=25,  tickInterval=0.5f },
        new ZonePhase { targetRadius=  3f, shrinkTime=50f,  holdTime=0f,  damagePerTick=50,  tickInterval=0.5f },
    };

    [Header("Zone Center")]
    [SerializeField] private Vector3 zoneCenter = Vector3.zero;
    [SerializeField] private float   startRadius = 180f;

    [Header("Visual")]
    [SerializeField] private Transform visualRing;

    // ── Networked ─────────────────────────────────────────────────────────
    public NetworkVariable<float> CurrentRadius = new(180f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int>   CurrentPhaseIndex = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ── Private ───────────────────────────────────────────────────────────
    private float _phaseTimer;
    private float _damageTimer;
    private float _fromRadius;
    private bool  _shrinking;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentRadius.Value     = startRadius;
            CurrentPhaseIndex.Value = 0;
            _fromRadius  = startRadius;
            _phaseTimer  = 0f;
            _shrinking   = false;
        }
    }

    private void Update()
    {
        if (IsServer && IsSpawned) ServerUpdate();
        UpdateVisual();
    }

    // ── Server-side logic ─────────────────────────────────────────────────
    private void ServerUpdate()
    {
        if (NetworkMatchManager.Instance == null ||
            NetworkMatchManager.Instance.Phase.Value != MatchPhase.InProgress) return;

        int phaseIdx = CurrentPhaseIndex.Value;
        if (phaseIdx >= phases.Count) return;

        ZonePhase phase = phases[phaseIdx];
        _phaseTimer += Time.deltaTime;

        if (!_shrinking)
        {
            // Hold period
            if (_phaseTimer >= phase.holdTime)
            {
                _phaseTimer = 0f;
                _shrinking  = true;
                _fromRadius = CurrentRadius.Value;
            }
        }
        else
        {
            // Shrink period
            float t = Mathf.Clamp01(_phaseTimer / Mathf.Max(1f, phase.shrinkTime));
            CurrentRadius.Value = Mathf.Lerp(_fromRadius, phase.targetRadius, t);

            if (_phaseTimer >= phase.shrinkTime)
            {
                CurrentRadius.Value     = phase.targetRadius;
                _phaseTimer             = 0f;
                _shrinking              = false;
                CurrentPhaseIndex.Value = Mathf.Min(phaseIdx + 1, phases.Count - 1);
            }
        }

        // Damage tick
        _damageTimer += Time.deltaTime;
        float tick = phase.tickInterval > 0f ? phase.tickInterval : 1f;
        if (_damageTimer >= tick)
        {
            _damageTimer = 0f;
            if (phase.damagePerTick > 0) DamagePlayersOutside(phase.damagePerTick, tick);
        }
    }

    private void DamagePlayersOutside(float dps, float dt)
    {
        int dmg = Mathf.CeilToInt(dps * dt);
        foreach (var hp in FindObjectsOfType<NetworkHealth>())
        {
            if (hp == null || !hp.IsAlive) continue;
            Vector3 p = hp.transform.position;
            float dist = Vector2.Distance(
                new Vector2(p.x, p.z),
                new Vector2(zoneCenter.x, zoneCenter.z));
            if (dist > CurrentRadius.Value)
                hp.ApplyDamageOnServer(dmg);
        }
    }

    // ── Visual (all clients) ──────────────────────────────────────────────
    private void UpdateVisual()
    {
        if (visualRing == null) return;
        float d = CurrentRadius.Value * 2f;
        visualRing.position    = zoneCenter;
        visualRing.localScale  = new Vector3(d, 0.05f, d);
    }

    // ── Public API ────────────────────────────────────────────────────────
    public bool IsInsideZone(Vector3 worldPos)
    {
        float dist = Vector2.Distance(
            new Vector2(worldPos.x, worldPos.z),
            new Vector2(zoneCenter.x, zoneCenter.z));
        return dist <= CurrentRadius.Value;
    }

    public Vector3 ZoneCenter    => zoneCenter;
    public Vector3 CurrentCenter => zoneCenter;   // alias used by MinimapSafeZoneRing
}
