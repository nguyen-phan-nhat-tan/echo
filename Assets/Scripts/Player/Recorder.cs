using System.Collections.Generic;
using UnityEngine;

public class Recorder : MonoBehaviour
{
    public List<FrameData> recordedFrames = new List<FrameData>();
    private PlayerController player;
    private bool isRecording = false;
    
    void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    public void StartRecording()
    {
        recordedFrames.Clear();
        isRecording = true;
    }

    public void StopRecording()
    {
        isRecording = false;
    }

    void FixedUpdate()
    {
        if (isRecording)
        {
            recordedFrames.Add(new FrameData(
                transform.position, 
                player.rotationAngle, 
                player.justShotTargetFrame,
                player.justDashedTargetFrame
            ));

            player.justShotTargetFrame = false;
            player.justDashedTargetFrame = false;
        }
    }
}