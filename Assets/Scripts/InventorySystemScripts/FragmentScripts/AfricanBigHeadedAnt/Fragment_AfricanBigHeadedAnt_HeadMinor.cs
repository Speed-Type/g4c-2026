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

        displayBugGroup = "Ant";
        displayBodyPart = "Head (Minor)";
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
