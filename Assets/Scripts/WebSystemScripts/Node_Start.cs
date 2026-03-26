using UnityEngine;
using System.Collections.Generic;

public class Node_Start : Node
{
    //Start node is where the player starts
    private bool hasVisited = false; //note that this variable does not interfere/interact with the Node parent class's hasBeenVisited variable

    protected override void Awake()
    {
        base.Awake();

        //MIGHT LEAD TO A BUG LATER??
        nodeType = NodeTypes.Start;
    }

    public override void VisitNode()
    {
        base.VisitNode();
        
        //Send request to NodeManager; if this node is not accessible, the rest of the function won't run
        if(!nodeManager.TryVisitNode(this))
        {
            return;
        }

        RoguelikeGameplayController.instance.SetOpenButtons(new List<PlayerOptionButtons> {PlayerOptionButtons.Map, PlayerOptionButtons.Inventory});
    }

    public override void DoNodeEffects()
    {
        LeanTween.delayedCall(RoguelikeCameraController.freeCamTransitionTiming + 2 * RoguelikeCameraController.generalDelayBetweenTransitions, () =>
        {
            //The following code is run AFTER camera has returned to free cam view and dialogue box is just about to open

            //Update node bg color
            SetNodeBGColorVisited();
            
            //Display flavor text, depending on whether the player has returned to this node or this is the first time they've been here
            if(hasVisited == false)
            {
                hasVisited = true;
                RoguelikeGameplayController.instance.dialogueBoxController.SetText(new List<string> {"Must feed.", "You should search for food."});
            }
            else
            {
                RoguelikeGameplayController.instance.dialogueBoxController.SetText(new List<string> {"This is where you started."});
            }
        });
    }
}
