using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

[RequireComponent(typeof(Volume))]
public class ExtremeImpactEffect : MonoBehaviour
{
    [Header("Settings")]
    public float duration = 0.5f;
    public float maxChromaticAberration = 1.0f;
    public float maxLensDistortion = -0.6f; 
    public bool useScalePunch = true;
    public float scalePunchAmount = 0.2f;

    private Volume _volume;
    private ChromaticAberration _chromaticAberration;
    private LensDistortion _lensDistortion;
    private Vector3 _initialScale;

    void Awake()
    {
        _volume = GetComponent<Volume>();
        _initialScale = transform.localScale;
        
        if (_volume == null) return;

        // Safely try to get profile settings
        if (_volume.profile != null)
        {
            if (!_volume.profile.TryGet(out _chromaticAberration))
            {
                // Optional: Debug.LogWarning("Chromatic Aberration missing from Volume Profile");
            }
            _volume.profile.TryGet(out _lensDistortion);
        }
    }

    void OnEnable()
    {
        if (_volume == null) return;
        StartCoroutine(ImpactRoutine());
    }

    IEnumerator ImpactRoutine()
    {
        float timer = 0f;

        // Punch Scale
        if (useScalePunch)
        {
            transform.localScale = _initialScale * (1f + scalePunchAmount);
        }

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; 
            float t = timer / duration;
            float curve = 1f - t; 

            // Check validity inside loop
            if (_volume == null) yield break;

            if (_chromaticAberration != null)
            {
                _chromaticAberration.active = true;
                _chromaticAberration.intensity.value = curve * maxChromaticAberration;
            }

            if (_lensDistortion != null)
            {
                _lensDistortion.active = true;
                _lensDistortion.intensity.value = curve * maxLensDistortion;
            }
            
            if (useScalePunch)
            {
                transform.localScale = Vector3.Lerp(_initialScale * (1f + scalePunchAmount), _initialScale, t);
            }

            yield return null;
        }

        CleanUp();
    }

    void OnDisable()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        // Strict null checks to prevent Editor errors on Destroy
        if (_volume == null || _volume.Equals(null)) return;
        if (_volume.profile == null || _volume.profile.Equals(null)) return;

        if (_chromaticAberration != null) _chromaticAberration.intensity.value = 0f;
        if (_lensDistortion != null) _lensDistortion.intensity.value = 0f;
        
        if (useScalePunch) transform.localScale = _initialScale;
    }
}
