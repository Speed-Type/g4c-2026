using UnityEngine;

public class Fragment_AfricanBigHeadedAnt_HeadMajor : Fragment
{
    public Sprite cardIcon;
    private Card card;
    
    public Fragment_AfricanBigHeadedAnt_HeadMajor()
    {
        shape = new bool[,]
        {
            {true, true},
            {true, true},
            {true, true}
        };

        maxHealth = 5;

        displayBugGroup = "Ant";
        displayBodyPart = "Head (Major)";
        displayCommonName = "African Big-Headed Ant";
        displayScientificName = "Pheidole megacephala";
        displayDescription = "Major worker ants of this species have abnormally large heads.\nChanges bite to one with larger area and greater damage.";
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

        if(!RoguelikeGameplayController.playerHasStartedLooting)
        {
            RoguelikeGameplayController.instance.equippedBites.Add(1);
            RoguelikeGameplayController.instance.UpdateBite();
        }
        
        return true;
    }

    public override bool DeactivateEffects()
    {
        if(base.DeactivateEffects() == false) return false;

        if(!RoguelikeGameplayController.playerHasStartedLooting)
        {
            RoguelikeGameplayController.instance.equippedBites.Remove(1);
            RoguelikeGameplayController.instance.UpdateBite();
        }

        return true;
    }
}
