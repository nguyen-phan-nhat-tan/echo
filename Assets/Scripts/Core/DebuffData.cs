using UnityEngine;

[CreateAssetMenu(fileName = "NewDebuff", menuName = "ScriptableObjects/DebuffData")]
public class DebuffData : ScriptableObject
{
    [Header("Identity")]
    public string debuffName = "Debuff";
    public string description = "A negative effect.";
    
    [Header("Multipliers (1.0 = Normal)")]
    // < 1.0 means stats are WORSE (slower move, slower fire).
    // EXCEPT: Dash Cooldown. Higher is worse?
    // Let's stick to "Multiplier". logic in PlayerController:
    // Speed * Mult. FireRate * Mult.
    // So 0.5 means half speed (bad). Half fire rate (bad).
    // For Dash Cooldown: In GameManager/PlayerController I decided: 
    // Cooldown / Mult. So 0.5 mult -> Cooldown / 0.5 = 2x Cooldown (Bad).
    // So consistently, < 1.0 is BAD (Debuff). > 1.0 is GOOD (Buff).
    
    public float moveSpeedMultiplier = 1f;
    public float fireRateMultiplier = 1f;
    public float dashCooldownMultiplier = 1f;
    
    [Header("Game State")]
    public float timerSpeedMultiplier = 1f; // > 1.0 means time runs faster (bad)
    
    [Header("Mechanics")]
    public bool drift = false; // Ice physics
    public bool fog = false;   // Turn on Vignette/Darkness
}
