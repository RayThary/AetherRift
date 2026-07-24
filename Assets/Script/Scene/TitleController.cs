using UnityEngine;

public class TitleController : MonoBehaviour
{
    public void StartGame()
    {
        SceneLoader.Instance.LoadScene(2);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
