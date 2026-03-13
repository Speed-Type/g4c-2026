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

    public override void ActivateEffects()
    {
        base.ActivateEffects();
    }

    public override void DeactivateEffects()
    {
        base.DeactivateEffects();
    }
}
