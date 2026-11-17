using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PingDisplay : NetworkBehaviour
{
    
    [Header("UI")]
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private Image    pingIcon;    

    [Header("Ping settings")]
    [SerializeField] private float measureInterval = 2f;    
    [SerializeField] private int   averageSamples  = 5;

    [Header("Thresholds (ms)")]
    [SerializeField] private int goodThreshold = 60;
    [SerializeField] private int badThreshold  = 120;

    [Header("Colours")]
    [SerializeField] private Color goodColor    = Color.green;
    [SerializeField] private Color mediumColor  = new Color(1f, 0.8f, 0f);
    [SerializeField] private Color badColor     = Color.red;

    
    private float   _measureTimer;
    private float   _echoPendingSendTime;
    private bool    _echoInFlight;
    private float[] _samples;
    private int     _sampleHead;
    private int     _sampleCount;

    
    public override void OnNetworkSpawn()
    {
        
        if (!IsOwner) { enabled = false; return; }

        _samples    = new float[averageSamples];
        _sampleHead = 0;
        _sampleCount = 0;
        UpdateDisplay(0);
    }

    private void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        _measureTimer += Time.unscaledDeltaTime;
        if (_measureTimer >= measureInterval && !_echoInFlight)
        {
            _measureTimer        = 0f;
            _echoInFlight        = true;
            _echoPendingSendTime = Time.unscaledTime;
            PingServerRpc();
        }
    }

    
    [ServerRpc]
    private void PingServerRpc(ServerRpcParams rpcParams = default)
    {
        
        PingResponseClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { rpcParams.Receive.SenderClientId }
            }
        });
    }

    [ClientRpc]
    private void PingResponseClientRpc(ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;

        float rtt = (Time.unscaledTime - _echoPendingSendTime) * 1000f; 
        _echoInFlight = false;

        _samples[_sampleHead] = rtt;
        _sampleHead           = (_sampleHead + 1) % averageSamples;
        _sampleCount          = Mathf.Min(_sampleCount + 1, averageSamples);

        float avg = 0f;
        for (int i = 0; i < _sampleCount; i++) avg += _samples[i];
        avg /= _sampleCount;

        UpdateDisplay(Mathf.RoundToInt(avg));
    }

    
    private void UpdateDisplay(int ms)
    {
        if (pingText == null) return;

        pingText.text = $"Ping: {ms} ms";

        Color c = ms < goodThreshold ? goodColor :
                  ms < badThreshold  ? mediumColor : badColor;

        pingText.color = c;
        if (pingIcon != null) pingIcon.color = c;
    }
}