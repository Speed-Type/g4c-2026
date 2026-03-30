using UnityEngine;

public class Fragment_AfricanBigHeadedAnt_HeadMajor : Fragment
{    
    public Fragment_AfricanBigHeadedAnt_HeadMajor()
    {
        shape = new bool[,]
        {
            {true, true},
            {true, true},
            {true, true}
        };

        maxHealth = 5;

        displayBugGroup = "Ant";
        displayBodyPart = "Head (Major)";
        displayCommonName = "African Big-Headed Ant";
        displayScientificName = "Pheidole megacephala";
        displayDescription = "Major worker ants of this species have abnormally large heads.\nChanges bite to one with larger area and greater damage.";
    }

    public override bool ActivateEffects()
    {
        if(base.ActivateEffects() == false) return false;

        if(!RoguelikeGameplayController.playerHasStartedLooting)
        {
            RoguelikeGameplayController.instance.equippedBites.Add(1);
            RoguelikeGameplayController.instance.UpdateBite();
        }
        
        return true;
    }

    public override bool DeactivateEffects()
    {
        if(base.DeactivateEffects() == false) return false;

        if(!RoguelikeGameplayController.playerHasStartedLooting)
        {
            RoguelikeGameplayController.instance.equippedBites.Remove(1);
            RoguelikeGameplayController.instance.UpdateBite();
        }

        return true;
    }
}
