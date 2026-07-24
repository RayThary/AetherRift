using UnityEngine;

public class PersistentSystems : MonoBehaviour
{
    private static PersistentSystems instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
