using UnityEngine;

public class Fragment_Test2 : Fragment
{
    void Start()
    {
        shape = new bool[,]
        {
            {true, true}
        };
    }

    public override bool ActivateEffects()
    {
        if(base.ActivateEffects() == false) return false;
        
        return true;
    }

    public override bool DeactivateEffects()
    {
        if(base.DeactivateEffects() == false) return false;
        
        return true;
    }
}
