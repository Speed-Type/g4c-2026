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

    public override void ActivateEffects()
    {
        base.ActivateEffects();
    }

    public override void DeactivateEffects()
    {
        base.DeactivateEffects();
    }
}
