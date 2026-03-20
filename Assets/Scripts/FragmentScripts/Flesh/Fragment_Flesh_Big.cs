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
        RoguelikeGameplayController.AddFlesh(4);

        //TODO: Do graphical effect

        Destroy(this.gameObject);
    }
    
    //Method to do the graphical pulsating
    void Pulse()
    {
        float scale = Random.Range(1.03f, 1.08f);
        float rotation = Random.Range(-1f,1f);
        float time = Random.Range(0.75f, 1.05f);

        LeanTween.scale(gameObject, Vector3.one * scale, time)
            .setEaseInOutSine()
            .setOnComplete(() =>
            {
                LeanTween.scale(gameObject, Vector3.one, time)
                    .setEaseInOutSine()
                    .setOnComplete(Pulse);
            });

        LeanTween.rotateZ(gameObject, rotation, time)
            .setEaseInOutSine().setOnComplete(() =>
            {
                LeanTween.rotateZ(gameObject, 0, time)
                    .setEaseInOutSine();
            });
    }
}
