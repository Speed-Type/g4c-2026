using UnityEngine;

public class Fragment_Fly_Thorax : Fragment
{
    public Sprite cardIcon;
    public Fragment_Fly_Thorax()
    {
        shape = new bool[,]
        {
            {true, true},
            {true, true}
        };

        fragmentEnum = Fragments.Fly_Thorax;

        maxHealth = 3;

        displayBugGroup = "Fly";
        displayBodyPart = "Thorax";
        displayCommonName = "True Fly";
        displayScientificName = "Diptera";
        displayDescription = "Like high-performance engines, powering wingbeats up to 1000 Hz.";
    }

    protected override void Awake()
    {
        base.Awake();

        card = new Card
        {
            cooldown = 10f,
            cardName = "Flap",
            cardSprite = cardIcon,
            cardDescription = "Jump",
            rarity = Card.Rarity.Uncommon,
            type = Card.Type.Speed,
            cardEffect = () => {SnakeGameplayController.instance.jumpQueued = true;}
        };
    }

    public override bool ActivateEffects()
    {
        if(base.ActivateEffects() == false) return false;

        Deck.instance.AddCard(card);
        
        return true;
    }

    public override bool DeactivateEffects()
    {
        if(base.DeactivateEffects() == false) return false;

        Deck.instance.RemoveCard(card);

        return true;
    }
}
