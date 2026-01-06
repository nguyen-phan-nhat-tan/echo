using UnityEngine;
using DG.Tweening;

public class SpawnRipple : MonoBehaviour
{
    [Header("Animation Settings")]
    public bool enableDropAnimation = true;
    public float dropHeight = 5f;
    public float dropDuration = 0.5f;
    public Ease dropEase = Ease.InExpo; // Accelerates like gravity

    [Header("Grid Impact")]
    public float impactForce = 15f;
    public float impactRadius = 5f;
    public Color impactColor = Color.cyan;
    
    [Header("Visuals (Optional)")]
    public ParticleSystem landParticles;

    private Vector3 initialScale;

    void Awake()
    {
        initialScale = transform.localScale;
    }

    void OnEnable()
    {
        if (enableDropAnimation)
        {
            PlayDropSequence();
        }
        else
        {
            // Immediate effect if no animation
            TriggerImpact();
        }
    }

    void PlayDropSequence()
    {
        // 1. Setup Start Pose
        // We simulate a drop by modifying position offset visually, 
        // OR simpler: Scale up from 2x (like jumping in) or Scale 0->1
        // Let's go with a "Slam" style: Start high, drop down.
        
        // Problem: Changing transform.position might fight with RigidBody or Spawn Logic.
        // Better approach for Top-Down 2D: "Scale Slam"
        // Start BIG (close to camera) and fade in, then slam to normal size.
        
        transform.localScale = initialScale * 3f; 
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if(sr != null) 
        {
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
            sr.DOFade(1f, dropDuration * 0.5f);
        }

        transform.DOScale(initialScale, dropDuration)
            .SetEase(dropEase)
            .SetLink(gameObject) // Fix: Link to object life
            .OnComplete(TriggerImpact);
    }

    void TriggerImpact()
    {
        // 1. Grid Ripple
        if (ReactiveGrid.Instance != null)
        {
            ReactiveGrid.Instance.ApplyForce(transform.position, impactForce, impactRadius, impactColor, true);
        }

        // 2. Camera Shake (via Event if needed, or local Feedback)
        // GameEvents.OnScreenShake?.Invoke(strength); // If we add this event later

        // 3. Particles
        if (landParticles != null)
        {
            landParticles.Play();
        }
    }
}
