using UnityEngine;

public class Fragment_Flesh_Big : Fragment
{
    public Fragment_Flesh_Big()
    {
        shape = new bool[,]
        {
            {true, true},
            {true, true}
        };

        maxHealth = 1;

        displayBugGroup = "Flesh";
        displayBodyPart = "";
        displayCommonName = "";
        displayScientificName = "";
        displayDescription = "You need it to grow.";
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
