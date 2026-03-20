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

    protected override void Awake()
    {
        base.Awake();

        Pulse(); //Start idle pulsating
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

    //Method to do the graphical pulsating
    void Pulse()
    {
        float scale = Random.Range(1.05f, 1.10f);
        float time = Random.Range(0.6f, 0.9f);

        LeanTween.scale(gameObject, Vector3.one * scale, time)
            .setEaseInOutSine()
            .setOnComplete(() =>
            {
                LeanTween.scale(gameObject, Vector3.one, time)
                    .setEaseInOutSine()
                    .setOnComplete(Pulse);
            });
    }
}
