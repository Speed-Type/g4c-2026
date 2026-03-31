using UnityEngine;

public class Fragment_Fly_Wing : Fragment
{
    public Sprite cardIcon;
    public Fragment_Fly_Wing()
    {
        shape = new bool[,]
        {
            {true, true},
            {true, true},
            {false, true}
        };

        fragmentEnum = Fragments.Fly_Wing;

        maxHealth = 2;

        displayBugGroup = "Fly";
        displayBodyPart = "Wing";
        displayCommonName = "True Fly";
        displayScientificName = "Diptera";
        displayDescription = "Quite the useful thing.";
    }

    protected override void Awake()
    {
        base.Awake();

        card = new Card
        {
            cooldown = 5f,
            cardName = "Fly",
            cardSprite = cardIcon,
            cardDescription = "Jump",
            rarity = Card.Rarity.Rare,
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
