using UnityEngine;

public class MobileJoystickBridge : MonoBehaviour
{
    [SerializeField] private NetworkPlayerController player;
    private Vector2 _currentMove;

    public void SendMove(Vector2 value)
    {
        _currentMove = Vector2.ClampMagnitude(value, 1f);
        Resolve();
        player?.SetMobileMove(_currentMove);
    }

    public void SendLook(Vector2 delta)
    {
        Resolve();
        player?.SetMobileLookDelta(delta);
    }

    public void ReleaseMove() => SendMove(Vector2.zero);

    private void Resolve()
    {
        if (player != null) return;
        foreach (var p in FindObjectsOfType<NetworkPlayerController>())
            if (p.IsOwner) { player = p; break; }
    }
}