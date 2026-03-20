using UnityEngine;
using UnityEngine.EventSystems;

public class Flesh_Big : Fragment_Flesh
{
    public Flesh_Big()
    {
        shape = new bool[,]
        {
            {true, true},
            {true, true}
        };
    }
}
