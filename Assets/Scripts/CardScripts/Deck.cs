using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    public static Deck instance;
    public List<Card> allCards; //List containing all cards
    public List<Card> drawPile;
    public List<Card> discardPile;


    void Awake()
    {
        instance = this;
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
        //Check if draw pile is empty, in which case shuffle first
        if(drawPile.Count == 0)
        {
            ShuffleDeck();
        }

        //Return and remove the first card in the drawPile
        Card temp = drawPile[0];
        discardPile.Add(temp);
        drawPile.RemoveAt(0);
        return temp;
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
