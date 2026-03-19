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

        displayBugGroup = "Ant";
        displayBodyPart = "Antenna";
        displayCommonName = "African Big-Headed Ant";
        displayScientificName = "Pheidole megacephala";
        displayDescription = "[Fun flavor text; this fragment's effect also here; just gonna put a bunch of words for now to test out text wrapping n stuff]";
    }

    public override void ActivateEffects()
    {
        base.ActivateEffects();
    }

    public override void DeactivateEffects()
    {
        base.DeactivateEffects();
    }
}
