using UnityEngine;
using UnityEngine.EventSystems;

public class Fragment_Flesh : Fragment
{
    public Fragment_Flesh()
    {
        shape = new bool[,]
        {
            {true, true},
            {true, true}
        };
    }

    protected override void Awake()
    {
        //Set dummy values, since this is derived from fragment
        maxHealth = 1;
        displayBugGroup = "Flesh";
        displayBodyPart = "";
        displayCommonName = "";
        displayScientificName = "";
        displayDescription = "You need it to grow.\nFragments can't be removed until their adjacent flesh is removed.";
        
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
        if(isDead) return; //only want to call this once
        isDead = true;
        
        //Add flesh to player
        RoguelikeGameplayController.AddFlesh(4);

        //Do graphical effect
        LeanTween.scale(gameObject, Vector3.one * 1.3f, 0.25f)
        .setEaseOutBack()
        .setOnComplete(() =>
        {
            RoguelikeCameraController.SpawnParticle(transform.position, deathParticles);
            Destroy(this.gameObject);
        });
    }

    //Just overriding to fix the bug where tooltip appears while flesh is mid-death animation
    public override void OnPointerEnter(PointerEventData eventData)
    {
        if(isDead) return;

        base.OnPointerEnter(eventData);
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
