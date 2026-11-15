using System.Collections;
using UnityEngine;

/// <summary>
/// Recoil and hit-feedback camera shake attached to the player camera pivot.
/// </summary>
public class CameraShake : MonoBehaviour
{
    [SerializeField] private float defaultIntensity = 0.3f;
    [SerializeField] private float defaultDuration  = 0.12f;

    private Vector3 _originalLocalPos;
    private Coroutine _shakeRoutine;

    private void Awake() => _originalLocalPos = transform.localPosition;

    public void Shake(float intensity = -1f, float duration = -1f)
    {
        float i = intensity < 0 ? defaultIntensity : intensity;
        float d = duration  < 0 ? defaultDuration  : duration;
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(DoShake(i, d));
    }

    private IEnumerator DoShake(float intensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float current = Mathf.Lerp(intensity, 0f, t);
            transform.localPosition = _originalLocalPos +
                new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * current;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = _originalLocalPos;
    }
}
