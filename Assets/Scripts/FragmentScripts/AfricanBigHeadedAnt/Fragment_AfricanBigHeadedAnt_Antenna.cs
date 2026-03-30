using UnityEngine;

public class Fragment_AfricanBigHeadedAnt_Antenna : Fragment
{
    public Fragment_AfricanBigHeadedAnt_Antenna()
    {
        shape = new bool[,]
        {
            {true, true}
        };

        maxHealth = 2;

        fragmentEnum = Fragments.AfricanBigHeadedAnt_Antenna;

        displayBugGroup = "Ant-enna";
        displayBodyPart = "";
        displayCommonName = "African Big-Headed Ant";
        displayScientificName = "Pheidole megacephala";
        displayDescription = "The primary navigation tool of the ant.\nReveal one extra Scavenge node upon moving nodes.";
    }

    public override bool ActivateEffects()
    {
        if(base.ActivateEffects() == false) return false;

        RoguelikeGameplayController.instance.scavengerNodesToReveal++;

        return true;
    }

    public override bool DeactivateEffects()
    {
        if(base.DeactivateEffects() == false) return false;

        RoguelikeGameplayController.instance.scavengerNodesToReveal--;

        return true;
    }
}
