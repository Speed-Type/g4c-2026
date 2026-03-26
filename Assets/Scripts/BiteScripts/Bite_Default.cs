using UnityEngine;
using UnityEngine.EventSystems;

public class Bite_Default : Fragment_Bite
{
    public Bite_Default()
    {
        shape = new bool[,] 
        {
            {true, true},
            {true, true}
        };

        damage = 1;
    }

    //All this function does is add screenshake
    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);

        //Screenshake
        ScreenShake.instance.AddTrauma(0.11f);
    }
}