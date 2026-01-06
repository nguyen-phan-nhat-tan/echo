using UnityEngine;
using UnityEngine.EventSystems;

public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType { Shoot, Dash }
    public ButtonType buttonType;
    public PlayerController player;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (player == null) return;

        if (buttonType == ButtonType.Shoot)
        {
            player.virtualFire = true;
        }
        else if (buttonType == ButtonType.Dash)
        {
            player.virtualDash = true; // Trigger dash
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (player == null) return;

        if (buttonType == ButtonType.Shoot)
        {
            player.virtualFire = false;
        }
        else if (buttonType == ButtonType.Dash)
        {
            player.virtualDash = false; 
        }
    }
}
