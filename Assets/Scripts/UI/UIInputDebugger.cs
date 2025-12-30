using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIInputDebugger : MonoBehaviour
{
    // Attach this to your Canvas or an empty GameObject in the scene
    
    [Header("Settings")]
    public bool showDebugLogs = true;
    
    // Internal pointer data
    PointerEventData pointerData;
    List<RaycastResult> results;

    void Start()
    {
        pointerData = new PointerEventData(EventSystem.current);
        results = new List<RaycastResult>();
    }

    void Update()
    {
        // Check for Mouse Click (Left) or Touch
        if (Input.GetMouseButtonDown(0))
        {
            pointerData.position = Input.mousePosition;
            results.Clear();

            // Cast a ray into the UI system
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"<color=cyan>UI CLICK DETECTED:</color>");
                    foreach (var result in results)
                    {
                        // Print the name of everything we clicked through
                        Debug.Log($"- Hit: <b>{result.gameObject.name}</b> (Depth: {result.depth})");
                    }
                }
            }
            else
            {
                if (showDebugLogs) Debug.Log("<color=orange>Click hit NO UI elements (World Space?)</color>");
            }
        }
    }
}