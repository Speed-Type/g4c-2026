using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Deck : MonoBehaviour
{
    public static Deck instance;

    [Header("Card Lists")]
    public List<Card> allCards = new(); //List containing all cards
    public List<Card> drawPile = new();
    public List<Card> discardPile = new();

    [Header("Default card")]
    public Sprite defaultCardSprite;
    private Card defaultCard;

    void Awake()
    {
        instance = this;

        //Create default card
        defaultCard = new Card
        {
            cooldown = 5f,
            cardName = "Resilient Growth",
            cardSprite = defaultCardSprite,
            cardDescription = "Immediately increase length by 1",
            rarity = Card.Rarity.Common,
            type = Card.Type.Growth,
            cardEffect = () => {SnakeGameplayController.instance.AddSegments(1);}
        };
    }

    //Resets deck so as to update drawPile and discardPile with cards
    public void ResetDeck()
    {
        drawPile.Clear();
        discardPile.Clear();

        //Fill discard pile with cards
        foreach(Card card in allCards)
        {
            discardPile.Add(card);
        }

        ShuffleDeck(); //Shuffle to move back into draw pile in randomized order
    }

    //Method to shuffle all cards back into draw pile
    public void ShuffleDeck()
    {
        //Move all draw cards into discard pile
        while(drawPile.Count != 0)
        {
            discardPile.Add(drawPile[0]);
            drawPile.RemoveAt(0);
        }

        //Randomize order of drawPile by adding randomly from discardPile
        while(discardPile.Count != 0)
        {
            int index = Random.Range(0, discardPile.Count);
            drawPile.Add(discardPile[index]);
            discardPile.RemoveAt(index);
        }
    }

    //Method that returns the first card of the deck
    public Card DrawCard()
    {
        Card cardToReturn;

        if(allCards.Count == 0) //Draw default if there are no cards remaining
        {
            cardToReturn = defaultCard;
        }
        else
        {
            //Check if draw pile is empty, in which case shuffle first
            if(drawPile.Count == 0)
            {
                ShuffleDeck();
            }

            //Return and remove the first card in the drawPile
            cardToReturn = drawPile[0];
            discardPile.Add(cardToReturn);
            drawPile.RemoveAt(0);
        }        

        return cardToReturn;
    }

    //Method to add a card to the complete deck
    public void AddCard(Card card)
    {
        allCards.Add(card);
    }

    //Method to completely remove a card from the complete deck
    public void RemoveCard(Card card)
    {
        allCards.Remove(card);
    }
}
