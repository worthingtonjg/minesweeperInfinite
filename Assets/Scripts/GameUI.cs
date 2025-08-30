using UnityEngine;
using UnityEngine.SceneManagement;


public class GameUI : MonoBehaviour
{
    public GameObject restartGameButton;

    public void ShowRestartGame()
    {
        restartGameButton.SetActive(true);
    }

    public void HideRestartGame()
    {
        restartGameButton.SetActive(false);
    }

    public void RestartGame()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }
}
