using Unity.Netcode;
using UnityEngine;

/// <summary>
/// When the local player dies, activates a free-roam spectator camera
/// that can cycle through alive players.
/// </summary>
public class SpectatorController : NetworkBehaviour
{
    [SerializeField] private float freeCamSpeed = 12f;
    [SerializeField] private float mouseSens    = 2f;

    private Camera _specCam;
    private bool   _spectating;
    private float  _yaw, _pitch;
    private NetworkHealth[] _targets;
    private int    _targetIndex;

    public override void OnNetworkSpawn()
    {
        // Create a spectator camera parented to this GO but inactive
        var camGo = new GameObject("SpectatorCamera");
        camGo.transform.SetParent(transform);
        camGo.transform.localPosition = new Vector3(0, 2f, -5f);
        _specCam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        camGo.SetActive(false);
    }

    // Called by NetworkHealth.NotifyDeathClientRpc on the owner
    public void EnterSpectate()
    {
        if (!IsOwner || _spectating) return;
        _spectating = true;

        // Disable player camera
        var ctrl = GetComponent<NetworkPlayerController>();
        if (ctrl != null && ctrl.PlayerCamera != null)
            ctrl.PlayerCamera.gameObject.SetActive(false);

        // Enable spectator cam
        if (_specCam != null)
        {
            _specCam.gameObject.SetActive(true);
            _specCam.transform.SetParent(null);
        }

        Cursor.lockState = CursorLockMode.Locked;
        RefreshTargets();
    }

    private void Update()
    {
        if (!IsOwner || !_spectating || _specCam == null) return;

        HandleFreeCam();
        HandleTargetCycle();
    }

    private void HandleFreeCam()
    {
        // Look
        _yaw   += Input.GetAxis("Mouse X") * mouseSens;
        _pitch -= Input.GetAxis("Mouse Y") * mouseSens;
        _pitch  = Mathf.Clamp(_pitch, -80f, 80f);
        _specCam.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // Move
        Vector3 move = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f),
            Input.GetAxisRaw("Vertical"));
        _specCam.transform.position += _specCam.transform.TransformDirection(move) * freeCamSpeed * Time.deltaTime;
    }

    private void HandleTargetCycle()
    {
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            RefreshTargets();
            if (_targets.Length == 0) return;
            _targetIndex = (_targetIndex + 1) % _targets.Length;
            _specCam.transform.position = _targets[_targetIndex].transform.position + new Vector3(0, 2, -4);
        }
    }

    private void RefreshTargets()
    {
        var list = new System.Collections.Generic.List<NetworkHealth>();
        foreach (var hp in FindObjectsOfType<NetworkHealth>())
            if (hp.IsAlive && hp.OwnerClientId != OwnerClientId) list.Add(hp);
        _targets = list.ToArray();
    }
}
