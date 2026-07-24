using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void LoadScene(int sceneIndex)
    {
        if (sceneIndex < 0 ||
            sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"등록되지 않은 씬 인덱스입니다: {sceneIndex}");
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }

    public void ReloadCurrentScene()
    {
        int currentSceneIndex =
            SceneManager.GetActiveScene().buildIndex;

        LoadScene(currentSceneIndex);
    }
}
