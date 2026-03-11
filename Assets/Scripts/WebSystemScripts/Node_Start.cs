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

        LeanTween.delayedCall(NodeManager.playerTravelAnimTime, () =>
        {
            //The following code is run AFTER player has finished moving on map
            nodeManager.rgController.SetOpenButtons(new List<PlayerOptionButtons> {PlayerOptionButtons.Map});

            LeanTween.delayedCall(RoguelikeCameraController.freeCamTransitionTiming, () =>
            {
                //The following code is run AFTER camera has returned to free cam view

                //Display flavor text, depending on whether the player has returned to this node or this is the first time they've been here
                if(hasVisited == false)
                {
                    hasVisited = true;
                    nodeManager.rgController.dialogueBoxController.SetText(new List<string> {"Must feed."});
                }
                else
                {
                    nodeManager.rgController.dialogueBoxController.SetText(new List<string> {"This is where you started."});
                }
            });
        });
    }
}
