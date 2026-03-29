using UnityEngine;

public class Fragment_AfricanBigHeadedAnt_Abdomen : Fragment
{
    public Sprite cardIcon;
    public Fragment_AfricanBigHeadedAnt_Abdomen()
    {
        shape = new bool[,]
        {
            {true, true},
            {true, true}
        };

        maxHealth = 3;

        displayBugGroup = "Ant";
        displayBodyPart = "Abdomen";
        displayCommonName = "African Big-Headed Ant";
        displayScientificName = "Pheidole megacephala";
        displayDescription = "These kinds of ants have crop stomachs for carrying lots of goodies.";
    }

    protected override void Awake()
    {
        base.Awake();

        card = new Card
        {
            cooldown = 12f,
            cardName = "Crop Stomach",
            cardSprite = cardIcon,
            cardDescription = "Instantly increase length by 3",
            rarity = Card.Rarity.Uncommon,
            cardEffect = () => {SnakeGameplayController.instance.AddSegments(3);}
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
