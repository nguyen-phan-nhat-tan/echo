using System;
using UnityEngine;

public enum GameState
{
    Intro,
    Playing,
    LoopTransition,
    Rewinding,
    GameOver
}

public static class GameEvents
{
    // Existing Events (Kept)
    public static Action OnPlayerDeath;
    public static Action OnEnemyDeath;
    
    // Lifecycle
    public static Action<int> OnLoopStart;
    public static Action OnLoopCompleted;
    public static Action OnLoopEnded;
    
    // State & UI
    public static Action<GameState> OnStateChanged;
    public static Action<int> OnScoreChanged;
    public static Action<float> OnTimerUpdate;
    public static Action<WeaponData> OnWeaponChange; 
}