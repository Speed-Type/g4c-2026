using System;
using UnityEngine;

public class Card
{
    public float cooldown;
    public string cardName;
    public Sprite cardSprite;
    public string cardDescription;
    private Action cardEffect;

    public void SetEffect(Action action)
    {
        cardEffect = action;
    }
    
    //Method to activate this card's effects
    public void ActivateCard()
    {
        cardEffect();
    }
}
