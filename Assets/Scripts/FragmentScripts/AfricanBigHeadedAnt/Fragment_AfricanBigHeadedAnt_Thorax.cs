using UnityEngine;

public class Fragment_AfricanBigHeadedAnt_Thorax : Fragment
{
    public Sprite cardIcon;

    public Fragment_AfricanBigHeadedAnt_Thorax()
    {
        shape = new bool[,]
        {
            {true, true}
        };

        maxHealth = 3;

        displayBugGroup = "Ant";
        displayBodyPart = "Thorax";
        displayCommonName = "African Big-Headed Ant";
        displayScientificName = "Pheidole megacephala";
        displayDescription = "Built for scavenging and carrying.";
    }

    protected override void Awake()
    {
        base.Awake();

        card = new Card
        {
            cooldown = 5f,
            cardName = "Sturdy Frame",
            cardSprite = cardIcon,
            cardDescription = "Immediately increase length by 1",
            rarity = Card.Rarity.Uncommon,
            type = Card.Type.Growth,
            cardEffect = () => {SnakeGameplayController.instance.AddSegments(1);}
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
