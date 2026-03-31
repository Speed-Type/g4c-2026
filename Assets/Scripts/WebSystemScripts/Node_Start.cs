using UnityEngine;
using System.Collections.Generic;

public class Node_Start : Node
{
    //Start node is where the player starts
    private bool hasVisited = false; //note that this variable does not interfere/interact with the Node parent class's hasBeenVisited variable

    private List<string> outText = new List<string>();

    protected override void Awake()
    {
        base.Awake();

        //MIGHT LEAD TO A BUG LATER??
        nodeType = NodeTypes.Start;

        switch(PlayerPrefs.GetInt("CurrentStage", 0))
        {
            case 0:
                outText.Add("This is a bone collector.");
                outText.Add("They're called that because scientists don't know just what to call them yet.");
                outText.Add("And I guess there's also the part where they collect the body parts of other bugs.");
                outText.Add("Why?");
                outText.Add("You'll see soon.");
                outText.Add("For now though, you are the bone collector.");
                outText.Add("The bone collector is you.");
                outText.Add("And you feel hungry.");
                outText.Add("You always feel hungry.");
                outText.Add("You should try and find something to eat.");
                outText.Add("You get the feeling that that something might be another bug.");
                break;
            case 2:
                outText.Add("Here you are again.");
                outText.Add("Caterpillars go through a number of instars before they reach metamorphosis.");
                outText.Add("Think of it as level-ups (Check your case when you can).");
                outText.Add("You're not the only thing that's grown bigger. You swear the web is a bit wider. Maybe your spider landlord has been busy.");
                outText.Add("Oh well. It's time to feed anyway.");
                outText.Add("Though you get the feeling that your feasting might get a little dangerous today...");
                break;
            case 4:
                outText.Add("This is it.");
                outText.Add("You can feel it.");
                outText.Add("It's time to leave behind your cloak of death and fly free.");
                outText.Add("You just need to eat a little bit more. It's the final stretch now.");
                break;
        }

        outText.Add("...");
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
                RoguelikeGameplayController.instance.dialogueBoxController.SetText(outText);
            }
            else
            {
                RoguelikeGameplayController.instance.dialogueBoxController.SetText(new List<string> {"This is where you started."});
            }
        });
    }
}
