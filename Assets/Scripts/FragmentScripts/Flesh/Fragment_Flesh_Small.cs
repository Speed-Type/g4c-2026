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

        //Do graphical effect
        LeanTween.scale(gameObject, Vector3.one * 1.6f, 0.25f)
        .setEaseOutBack()
        .setOnComplete(() =>
        {
            RoguelikeCameraController.SpawnParticle(transform.position, deathParticles);
            Destroy(this.gameObject);
        });
    }

    //Method to do the graphical pulsating
    void Pulse()
    {
        float scale = Random.Range(1.03f, 1.08f);
        float rotation = Random.Range(-1.5f,1.5f);
        float time = Random.Range(0.7f, 1.0f);

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
