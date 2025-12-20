using System.Collections.Generic;

[System.Serializable]
public class LoopData
{
    public int weaponIndex;
    public List<FrameData> frames;

    public LoopData(int index, List<FrameData> recording)
    {
        weaponIndex = index;
        frames = recording;
    }
}