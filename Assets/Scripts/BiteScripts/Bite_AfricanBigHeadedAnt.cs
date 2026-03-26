using UnityEngine;
using UnityEngine.EventSystems;

public class Bite_AfricanBigHeadedAnt : Fragment_Bite
{
    public Bite_AfricanBigHeadedAnt()
    {
        shape = new bool[,] 
        {
            {true, true, true, true, true},
            {true, true, true, true, true},
            {true, true, true, true, true},
            {true, true, true, true, true}
        };

        damage = 3;
    }

    //All this function does is add screenshake
    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);

        //Screenshake
        ScreenShake.instance.AddTrauma(0.05f);
    }
}
