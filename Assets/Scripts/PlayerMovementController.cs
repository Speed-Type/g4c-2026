using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class PlayerMovementController : MonoBehaviour
{
    //Input actions
    private InputAction moveAction;
    private InputAction utilityAction;

    //Base class initialization
    public virtual void Start()
    {
        //Setup movement and add functions
        moveAction = InputSystem.actions.FindAction("Move");
        //TODO: bind functions for movement with WASD

        utilityAction = InputSystem.actions.FindAction("Sprint");
        utilityAction.started += 
            context =>
            {
                if(context.interaction is SlowTapInteraction)
                    ContinueMove(moveAction.ReadValue<Vector2>());

                //TODO: finish binding functions for utility moves
                //List of default interactions: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/manual/Interactions.html
                //Quickstart guide (not super helpful for this kind of implementation) https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/manual/QuickStartGuide.html
            };
    }

    //Base class updating
    public virtual void Update()
    {
        
    }

    //Method to start WASD movement
    //Should be completely overridden in child classes
    public virtual void StartMove(Vector2 inputValue)
    {
        
    }

    //Method to start WASD movement
    //Should be completely overridden in child classes
    public virtual void ContinueMove(Vector2 inputValue)
    {
        
    }

    //Method to start WASD movement
    //Should be completely overridden in child classes
    public virtual void EndMove(Vector2 inputValue)
    {
        
    }

    //Dash method
    //Should be completely overridden in child classes
    public virtual void Utility()
    {
        
    }
}
