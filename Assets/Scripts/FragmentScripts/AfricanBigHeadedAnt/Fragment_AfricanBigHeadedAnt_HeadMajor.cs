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
        displayDescription = "[Fun flavor text; this fragment's effect also here; just gonna put a bunch of words for now to test out text wrapping n stuff]";
    }

    public override bool ActivateEffects()
    {
        if(base.ActivateEffects() == false) return false;
        
        return true;
    }

    public override bool DeactivateEffects()
    {
        if(base.DeactivateEffects() == false) return false;

        return true;
    }
}
