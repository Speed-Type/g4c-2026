using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField]
    private Fragment currentFragment;
    public bool isOnEdge = false;

    [Header("Placement Highlight Graphics")]
    [SerializeField] private Image highlightGraphic;
    [SerializeField] private Color noHighlightColor;
    [SerializeField] private Color validHighlightColor;
    [SerializeField] private Color invalidHighlightColor;

    //Mutator methods
    public void SetFragment(Fragment newFragment)
    {
        currentFragment = newFragment;
    }

    //Accessor methods
    public Fragment GetFragment()
    {
        return currentFragment;
    }

    //Methods for higlighting this slot
    public void ResetHighlight()
    {
        highlightGraphic.color = noHighlightColor;
    }

    public void HighlightValid()
    {
        highlightGraphic.color = validHighlightColor;
    }

    public void HighlightInvalid()
    {
        highlightGraphic.color = invalidHighlightColor;
    }
}