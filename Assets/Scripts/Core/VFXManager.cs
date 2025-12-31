using UnityEngine;

public class VFXManager : MonoBehaviour
{
    [Header("Pool Tags")]
    public string impactTag = "VFX_Impact";
    public string explosionTag = "VFX_Explosion";

    void OnEnable()
    {
        GameEvents.OnBulletImpact += HandleBulletImpact;
        GameEvents.OnEnemyExplosion += HandleEnemyExplosion;
    }

    void OnDisable()
    {
        GameEvents.OnBulletImpact -= HandleBulletImpact;
        GameEvents.OnEnemyExplosion -= HandleEnemyExplosion;
    }

    private void HandleBulletImpact(Vector2 pos, Quaternion rotation)
    {
        // Spawns sparks flying AWAY from the wall (based on rotation passed in)
        ObjectPooler.Instance.SpawnFromPool(impactTag, pos, rotation);
    }

    private void HandleEnemyExplosion(Vector2 pos)
    {
        // Spawns a 360 burst
        ObjectPooler.Instance.SpawnFromPool(explosionTag, pos, Quaternion.identity);
    }
}