using System;
using UnityEngine;

public class Card
{
    public float cooldown;
    public string cardName;
    public Sprite cardSprite;
    public string cardDescription;
    public Action cardEffect;

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    public Rarity rarity;
    
    //Method to activate this card's effects
    public void ActivateCard()
    {
        cardEffect();
    }
}
