using UnityEngine;

public class Fragment_Flesh_Small : Fragment
{
    public Fragment_Flesh_Small()
    {
        shape = new bool[,]
        {
            {true}
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
        RoguelikeGameplayController.AddFlesh(1);

        //TODO: Do graphical effect

        Destroy(this.gameObject);
    }
}
