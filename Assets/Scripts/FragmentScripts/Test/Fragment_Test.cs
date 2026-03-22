using UnityEngine;

public class Fragment_Test : Fragment
{
    void Start()
    {
        shape = new bool[,]
        {
            {true, true, true, true},
            {true, false, false, false}
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
