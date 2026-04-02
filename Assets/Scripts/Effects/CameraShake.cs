using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class CameraShake : MonoBehaviour
{
    static CinemachineImpulseSource _impulseSource;
    static Coroutine _activeShake;

    public static void Shake(MonoBehaviour context, float durationMs, float intensity)
    {
        if (context == null) return;

        if (_impulseSource == null)
            _impulseSource = Object.FindFirstObjectByType<CinemachineImpulseSource>();

        if (_impulseSource != null)
        {
            _impulseSource.DefaultVelocity = new Vector3(intensity, intensity, 0f);
            _impulseSource.ImpulseDefinition.ImpulseDuration = durationMs / 1000f;
            _impulseSource.GenerateImpulse();
            return;
        }

        // Fallback: direct camera shake if no Cinemachine setup
        var cam = Camera.main;
        if (cam == null) return;

        if (_activeShake != null)
            context.StopCoroutine(_activeShake);

        _activeShake = context.StartCoroutine(DoShake(cam.transform, durationMs / 1000f, intensity));
    }

    static IEnumerator DoShake(Transform camTransform, float duration, float intensity)
    {
        Vector3 originalPos = camTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / duration);
            float offsetX = Random.Range(-1f, 1f) * intensity * t;
            float offsetY = Random.Range(-1f, 1f) * intensity * t;
            camTransform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }

        camTransform.localPosition = originalPos;
        _activeShake = null;
    }
}
