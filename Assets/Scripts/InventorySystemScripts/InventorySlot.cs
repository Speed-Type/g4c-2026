using UnityEngine;

public class InventorySlot : MonoBehaviour
{
    [SerializeField]
    private Fragment currentFragment;
    public bool isOnEdge = false;

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
}
