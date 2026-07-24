using UnityEngine;

public class LobbyController : MonoBehaviour
{
    
    public void StartBattle()
    {
        SceneLoader.Instance.LoadScene(3);
    }

    public void ReturnTitle()
    {
        SceneLoader.Instance.LoadScene(1);
    }
}
