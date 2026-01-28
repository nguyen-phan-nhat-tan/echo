using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "ScriptableObjects/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Stats")]
    public string weaponName = "Pistol";
    public Sprite weaponSprite;
    
    public float fireRate = 5f;
    public int bulletCount = 1;
    public float spreadAngle = 0f;
    
    [Header("Burst Settings")]
    public int burstCount = 1; 
    public float burstDelay = 0.05f;

    [Header("Patterns & Mechanics")]
    public bool ricochet = false;
    public bool waveMovement = false;
    public float spiralRate = 0f; // Degrees per shot
    public int fixedPatternCount = 0; // If > 0, overrides random spread with fixed angles
    
    public float damage = 1f; 
    public float bulletSpeed = 10f; 

    
    [Header("Visuals")]
    public string bulletTag = "PlayerBullet";
    public float shakeIntensity = 0.2f;
    
    [Header("VFX Prefabs")]
    public GameObject muzzleFlashPrefab;
    public GameObject bulletTrailPrefab;
    public GameObject wallHitPrefab;
    public GameObject enemyHitPrefab;
    
    [Header("Audio")]
    public AudioClip shootClip;
}