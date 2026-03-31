using UnityEngine;

public class Fragment_Fly_Halteres : Fragment
{
    public Fragment_Fly_Halteres()
    {
        shape = new bool[,]
        {
            {true}
        };

        fragmentEnum = Fragments.Fly_Haltere;

        maxHealth = 1;

        displayBugGroup = "Fly";
        displayBodyPart = "Haltere";
        displayCommonName = "True Fly";
        displayScientificName = "Diptera";
        displayDescription = "Halteres are stabilizing organs that help flies, well, fly.\nGrants substantial move speed.";
    }

    protected override void Awake()
    {
        base.Awake();
    }

    public override bool ActivateEffects()
    {
        if(base.ActivateEffects() == false) return false;

        SnakeGameplayController.instance.startingMoveSpeed++;
        
        return true;
    }

    public override bool DeactivateEffects()
    {
        if(base.DeactivateEffects() == false) return false;

        SnakeGameplayController.instance.startingMoveSpeed--;

        return true;
    }
}
