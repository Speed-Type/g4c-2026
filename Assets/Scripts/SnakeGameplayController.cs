using UnityEngine;

public class SnakeGameplayController : MonoBehaviour
{
    private bool isPlaying = false;
    public void PlayGame()
    {
        isPlaying = true;

        //TEMP
        LeanTween.delayedCall(3f, () =>
        {
            EndGame(true);
        });
    }

    public void EndGame(bool playerWon)
    {
        isPlaying = false;
        
        //On finish, call switch camera in cam controller
        RoguelikeGameplayController.instance.cameraController.SwitchBackFromBattleCam(playerWon);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
