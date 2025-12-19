using UnityEngine;

public class UnderwaterMovementScript : PlayerMovementController
{
    public float movementSpeed;
    public override void Start()
    {
        base.Start();
        
    }

    public override void Update()
    {
        base.Update();
        
    }

    //TODO: override other necessary methods
    public override void ContinueMove(Vector2 inputValue)
    {
        transform.Translate(inputValue * Time.deltaTime * movementSpeed);
    }


}
