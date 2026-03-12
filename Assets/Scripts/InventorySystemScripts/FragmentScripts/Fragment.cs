using UnityEngine;
using UnityEngine.EventSystems;

public class Fragment : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string fragmentName;
    public Sprite icon;
    public bool[,] shape; //will be initialized in subclasses

    public bool isOnEdge;

    //Internal logic
    private RectTransform rectTransform;
    private Canvas canvas;
    private InventoryManager inventoryManager;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        inventoryManager = FindFirstObjectByType<InventoryManager>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //When drag begins, make it the topmost item
        transform.SetAsLastSibling();

        //Remove from inventory manager
        inventoryManager.RemoveFragment(this);
    }

    //Actual dragging functionality
    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //Snap to grid
        inventoryManager.SnapToGrid(this);
    }

    //Method to rotate 90 degrees clockwise
    public void Rotate()
    {
        //Need to transpose matrix and then reverse each column
        int width = shape.GetLength(0);
        int height = shape.GetLength(1);

        bool[,] newShape = new bool[height, width]; //reversed dimensions

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int newRow = width - 1 - j;
                int newCol = height - 1 - i;
                newShape[newRow, newCol] = shape[j, i];
            }
        }

        shape = newShape;

        //Rotate visually
        transform.Rotate(0,0,90);
    }

    //Method to be overridden in children
    //The effects of a fragment should be here, as this is called when a fragment is equipped
    public virtual void ActivateEffects()
    {
        
    }

    //Method to be overridden in children
    //Should deactivate the effects of a fragment, as this is called when a fragment is unequipped
    public virtual void DeactivateEffects()
    {
        
    }
}
