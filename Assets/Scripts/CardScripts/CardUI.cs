using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    [Header("Card UI")]
    public Image cardBackgroundImage;
    public Sprite[] backgroundSpriteList;
    public TMP_Text cardTitleText;
    public Image cardIconImage;
    public TMP_Text cardDescriptionText;
    public Image cardBorderImage;
    public Sprite[] borderSpriteList;
    public TMP_Text cooldownText;

    public void UpdateCardDisplay(Card card)
    {
        cardBackgroundImage.sprite = backgroundSpriteList[(int)card.type];
        cardTitleText.text = card.cardName;
        cardIconImage.sprite = card.cardSprite;
        cardDescriptionText.text = card.cardDescription;
        cardBorderImage.sprite = borderSpriteList[(int)card.rarity];
        cooldownText.text = card.cooldown.ToString("F2");
    }
}
