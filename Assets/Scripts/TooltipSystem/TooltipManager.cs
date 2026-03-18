using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class TooltipManager : MonoBehaviour
{
    private static TooltipManager instance;
    public TextMeshProUGUI TooltipText; //Text within the tooltip
    public RectTransform TooltipBackground; //Black bg of the tooltip
    public GameObject TooltipObject; //Tooltip object housing the two above

    void Awake()
    {
        instance = this;
        HideTooltip_Static();
    }

    void Update()
    {
        //dynamically position and anchor the tooltip based on mouse position
        Vector3 screenPoint = Mouse.current.position.ReadValue();
        TooltipObject.GetComponent<RectTransform>().position = screenPoint;
    }

    //Shows tooltip with set text
    public void ShowTooltip(string content)
    {

        TooltipText.text = content;
        float padding = 4f; //padding to be applied to the tooltip
        Vector2 bgSize = new Vector2(TooltipText.preferredWidth + (padding * 2f), TooltipText.preferredHeight + (padding * 2f));
        TooltipBackground.sizeDelta = bgSize; // resize the tooltip background

        TooltipObject.SetActive(true);

    }

    //Hides tooltip
    public void HideTooltip()
    {
        TooltipObject.SetActive(false);
    }

    public static void ShowTooltip_Static(string content)
    {
        instance.ShowTooltip(content);
    }

    public static void HideTooltip_Static()
    {
        instance.HideTooltip();
    }
}
