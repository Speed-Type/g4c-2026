using UnityEngine;

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
}
