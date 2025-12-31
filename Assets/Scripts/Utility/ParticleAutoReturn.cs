using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleAutoReturn : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.Callback; // Critical setting
    }

    void OnEnable()
    {
        ps.Play();
    }

    // Called automatically by Unity when particle finishes
    void OnParticleSystemStopped()
    {
        gameObject.SetActive(false); // Return to pool
    }
}