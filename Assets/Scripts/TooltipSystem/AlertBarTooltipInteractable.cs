using UnityEngine;
using UnityEngine.EventSystems;

public class AlertBarTooltipInteractable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        //Determine what to display for fragment health
        string output = "Spider Alertness: " + RoguelikeGameplayController.instance.currentSpiderAlertness + "/" + RoguelikeGameplayController.instance.maxSpiderAlertness + "\n";

        if(RoguelikeGameplayController.instance.currentSpiderAlertness >= RoguelikeGameplayController.instance.maxSpiderAlertness)
        {
            //Spider is on the hunt
            output += "WARNING: Spider is on the hunt\n";
            TooltipManager.ColorRed_Static();
        }
        else
        {
            TooltipManager.ColorDefault_Static();
        }

        output += "\nAffects how often the Spider actively pursues you";

        TooltipManager.ShowTooltip_Static(output);
    }

    //Method for stopping tooltip hovering
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.HideTooltip_Static();
    }
}
