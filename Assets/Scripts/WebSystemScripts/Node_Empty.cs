using UnityEngine;
using System.Collections.Generic;

public class Node_Empty : Node
{
    //Empty node just has the player continue after showing some flavor text
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
