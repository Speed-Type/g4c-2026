using UnityEngine;
using System.Collections.Generic;

public class Node_Empty : Node
{
    //Empty node just has the player continue after showing some flavor text
    public override void VisitNode()
    {
        base.VisitNode();

        //Update open buttons
        nodeManager.rgController.AddOpenButtons(new List<PlayerOptionButtons> {PlayerOptionButtons.Map});
    }
}
