using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct FrameData
{
    public Vector2 position;
    public float rotation;
    public bool isShooting;

    public FrameData(Vector2 pos, float rot, bool shoot)
    {
        position = pos;
        rotation = rot;
        isShooting = shoot;
    }
}