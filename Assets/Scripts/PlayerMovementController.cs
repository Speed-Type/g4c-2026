using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementController : MonoBehaviour
{
    //Input actions
    private InputAction moveAction;

    //Base class initialization
    public virtual void Start()
    {
        //Initialize move actions
        moveAction = InputSystem.actions.FindAction("Move");
    }

    //Base class updating (Mainly just controls movement function calling)
    public virtual void Update()
    {
        //Move
        Move(moveAction.ReadValue<Vector2>());
    }

    //Movement method that should be completely overridden in child classes
    public virtual void Move(Vector2 inputValue)
    {
        
    }
}
