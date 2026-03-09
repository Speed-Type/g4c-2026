using UnityEngine;
using System.Collections.Generic;

public class Node_Start : Node
{
    //Start node is where the player starts
    public override void VisitNode()
    {
        //Send request to NodeManager; if this node is not accessible, the rest of the function won't run
        if(!nodeManager.TryVisitNode(this))
        {
            return;
        }

        //Update open buttons
        nodeManager.rgController.SetOpenButtons(new List<PlayerOptionButtons> {PlayerOptionButtons.Map});
    }
}
