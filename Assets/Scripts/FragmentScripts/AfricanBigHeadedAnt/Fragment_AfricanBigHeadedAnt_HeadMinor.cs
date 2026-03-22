using UnityEngine;

public class Fragment_AfricanBigHeadedAnt_HeadMinor : Fragment
{
    public Fragment_AfricanBigHeadedAnt_HeadMinor()
    {
        shape = new bool[,]
        {
            {true, true},
            {true, true}
        };

        maxHealth = 4;

        displayBugGroup = "Ant";
        displayBodyPart = "Head (Minor)";
        displayCommonName = "African Big-Headed Ant";
        displayScientificName = "Pheidole megacephala";
        displayDescription = "These ants are dimorphic, split between major workers (soldiers) and minor workers.\nIncrease number of bites by 1.";
    }

    public override bool ActivateEffects()
    {
        if(base.ActivateEffects() == false) return false;
        
        RoguelikeGameplayController.instance.maxBites++;

        return true;
    }

    public override bool DeactivateEffects()
    {
        if(base.DeactivateEffects() == false) return false;

        RoguelikeGameplayController.instance.maxBites--;

        return true;
    }
}
