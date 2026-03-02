using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int selectedTheme = 0; // 0-4 untuk tema 1-5
    public bool isRestarting = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
