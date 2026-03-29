using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    [Header("Card UI")]
    public TMP_Text cardTitleText;
    public Image cardIconImage;
    public TMP_Text cardDescriptionText;
    public Image cardBorderImage;
    public Sprite[] borderSpriteList;

    public void UpdateCardDisplay(Card card)
    {
        cardTitleText.text = card.cardName;
        cardIconImage.sprite = card.cardSprite;
        cardDescriptionText.text = card.cardDescription;
        cardBorderImage.sprite = borderSpriteList[(int)card.rarity];
    }
}
