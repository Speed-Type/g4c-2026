using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//Abstract base class that every other app will inherit from
public abstract class AppBase : MonoBehaviour
{
    //Reference to the OS, set when the app is launched
    protected ComputerOS OS {get; private set;}

    //Called by ComputerOS immediately after the window is instantiated
    public virtual void OnAppOpen(ComputerOS os)
    {
        OS = os;
    }

    //Called by ComputerOS just before the window is destroyed
    public virtual void OnAppClose() { }
}
