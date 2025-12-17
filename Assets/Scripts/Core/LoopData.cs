using System.Collections.Generic;

[System.Serializable]
public class LoopData
{
    public int weaponIndex; // Which gun was used?
    public List<FrameData> frames; // The recording

    public LoopData(int index, List<FrameData> recording)
    {
        weaponIndex = index;
        frames = recording;
    }
}