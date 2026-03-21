using UnityEngine;
using System.Collections.Generic;

public class Node_Scavenge : Node
{
    //Scavenge node contains loot for the player

    [Header("Odds Corresponding to BugLoot Enum")]
    public int[] bugLootOdds;

    protected override void Awake()
    {
        base.Awake();

        nodeType = NodeTypes.Scavenge;
    }

    public override void VisitNode()
    {
        //Send request to NodeManager; if this node is not accessible, the rest of the function won't run
        if(!nodeManager.TryVisitNode(this))
        {
            return;
        }
        
        nodeManager.rgController.SetOpenButtons(new List<PlayerOptionButtons> {PlayerOptionButtons.Map, PlayerOptionButtons.Inventory, PlayerOptionButtons.Loot});
        
        //Generate loot

        //First, get the total odds
        int sum = 0;
        foreach(int odd in bugLootOdds)
        {
            sum += odd;
        }

        //Generate random number from [0, sum]
        float rand = Random.Range(0f, sum);
        float current = 0;

        for(int j = 0; j < bugLootOdds.Length; j++)
        {
            current += bugLootOdds[j];
            if(current >= rand)
            {
                //This is the correct index
                FindFirstObjectByType<LootManager>().GenerateLoot((LootTables)j);
                j = bugLootOdds.Length + 67;
            }
        }

        LeanTween.delayedCall(RoguelikeCameraController.freeCamTransitionTiming + 2 * RoguelikeCameraController.generalDelayBetweenTransitions + NodeManager.playerTravelAnimTime, () =>
        {
            //The following code is run AFTER camera has returned to free cam view and paused the general delay time

            //Update node bg color
            SetNodeBGColorVisited();
            
            //Display random flavor text
            //nodeManager.rgController.dialogueBoxController.SetText(new List<string> {flavorText[Random.Range(0, flavorText.Count)], "..."});
            nodeManager.rgController.dialogueBoxController.SetText(new List<string> {"Temp text for found bug", "..."});
        });
    }
}
