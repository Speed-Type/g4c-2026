using UnityEngine;
using System.Collections.Generic;

public class Node_Start : Node
{
    //Start node is where the player starts
    bool hasVisited = false;
    public override void VisitNode()
    {
        //Send request to NodeManager; if this node is not accessible, the rest of the function won't run
        if(!nodeManager.TryVisitNode(this))
        {
            return;
        }

        nodeManager.rgController.SetOpenButtons(new List<PlayerOptionButtons> {PlayerOptionButtons.Map, PlayerOptionButtons.Inventory});

        LeanTween.delayedCall(RoguelikeCameraController.freeCamTransitionTiming + 2 * RoguelikeCameraController.generalDelayBetweenTransitions + NodeManager.playerTravelAnimTime, () =>
        {
            //The following code is run AFTER camera has returned to free cam view and dialogue box is just about to open

            //Display flavor text, depending on whether the player has returned to this node or this is the first time they've been here
            if(hasVisited == false)
            {
                hasVisited = true;
                nodeManager.rgController.dialogueBoxController.SetText(new List<string> {"Must feed.", "You should search for food."});
            }
            else
            {
                nodeManager.rgController.dialogueBoxController.SetText(new List<string> {"This is where you started."});
            }
        });
    }
}
