using UnityEngine;

public class VFXManager : MonoBehaviour
{
    [Header("Pool Tags")]
    public string wallHitTag = "VFX_Impact";
    public string enemyHitTag = "VFX_Explosion";

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

    private void HandleBulletImpact(Vector2 pos, Quaternion rot)
    {
        // Spawns sparks matching the impact rotation
        ObjectPooler.Instance.SpawnFromPool(wallHitTag, pos, rot);
    }

    private void HandleEnemyExplosion(Vector2 pos)
    {
        // Spawns explosion at identity rotation
        ObjectPooler.Instance.SpawnFromPool(enemyHitTag, pos, Quaternion.identity);
    }
}