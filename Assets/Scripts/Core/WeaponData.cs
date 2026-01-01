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
    
    public float damage = 1f; 
    public float bulletSpeed = 10f; 

    
    [Header("Visuals")]
    public string bulletTag = "PlayerBullet";
    public float shakeIntensity = 0.2f;
    
    [Header("Audio")]
    public AudioClip shootClip;
}