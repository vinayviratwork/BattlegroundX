using UnityEngine;
using UnityEngine.EventSystems;

public class FireButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private NetworkPlayerController _player;
    private NetworkWeapon           _weapon;
    private bool _held;

    private void Update()
    {
        if (!_held) return;
        if (_weapon != null && _weapon.GetCurrentWeapon().isAutomatic)
            _weapon.TryFire();
    }

    public void OnPointerDown(PointerEventData e)
    {
        Find();
        _held = true;
        _weapon?.TryFire();
    }

    public void OnPointerUp(PointerEventData e) => _held = false;

    private void Find()
    {
        if (_player != null) return;
        foreach (var p in FindObjectsOfType<NetworkPlayerController>())
            if (p.IsOwner) { _player = p; _weapon = p.GetComponent<NetworkWeapon>(); break; }
    }
}