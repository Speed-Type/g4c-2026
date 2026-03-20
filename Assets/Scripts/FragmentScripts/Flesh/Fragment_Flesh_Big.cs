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
        displayDescription = "You need it to grow.\nFragments can't be removed until their adjacent flesh is removed.";
    }

    public override bool IsFlesh()
    {
        return true;
    }

    public override void TryFreeFragment()
    {
        //Do nothing; this fragment should not be movable to begin with
    }

    public override void Die()
    {
        //Add flesh to player
        RoguelikeGameplayController.AddFlesh(4);

        //TODO: Do graphical effect

        Destroy(this.gameObject);
    }
}
