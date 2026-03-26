using UnityEngine;

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
}
