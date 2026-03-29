using UnityEngine;

public class Fragment_AfricanBigHeadedAnt_Leg : Fragment
{
    public Sprite cardIcon;

    public Fragment_AfricanBigHeadedAnt_Leg()
    {
        shape = new bool[,]
        {
            {true, true}
        };

        maxHealth = 2;

        displayBugGroup = "Ant";
        displayBodyPart = "Leg";
        displayCommonName = "African Big-Headed Ant";
        displayScientificName = "Pheidole megacephala";
        displayDescription = "Built for scavenging and scurrying.";
    }

    protected override void Awake()
    {
        base.Awake();

        card = new Card
        {
            cooldown = 7.5f,
            cardName = "Robust Legs",
            cardSprite = cardIcon,
            cardDescription = "Slightly increase speed until the end of this combat",
            rarity = Card.Rarity.Common,
            cardEffect = () => {SnakeGameplayController.instance.moveSpeed+=0.1f;}
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
