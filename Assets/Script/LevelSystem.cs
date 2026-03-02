using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSystem : MonoBehaviour
{
    public GameObject[] levels;
    public GameObject winPage;

    private int currentLevel = 0;

    void Start()
    {
        int levelToLoad;

        if (GameManager.Instance != null && GameManager.Instance.isRestarting)
        {
            levelToLoad = PlayerPrefs.GetInt("CurrentLevel");
            GameManager.Instance.isRestarting = false;
        }
        else
        {
            levelToLoad = GameManager.Instance.selectedTheme;
        }

        ShowLevel(levelToLoad);
        winPage.SetActive(false);
    }

    void ShowLevel(int index)
    {
        for (int i = 0; i < levels.Length; i++)
        {
            levels[i].SetActive(i == index);
        }

        currentLevel = index;
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
    }

    public void LevelComplete()
    {
        winPage.SetActive(true);
    }

    public void NextLevel()
    {
        winPage.SetActive(false);

        if (currentLevel + 1 < levels.Length)
        {
            ShowLevel(currentLevel + 1);
        }
        else
        {
            GameManager.Instance.isRestarting = false;
            PlayerPrefs.DeleteKey("CurrentLevel");
            SceneManager.LoadScene("Main Menu"); // ganti sesuai nama scene kamu
        }
    }

    public void RestartLevel()
    {
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        GameManager.Instance.isRestarting = true;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        GameManager.Instance.isRestarting = false;
        SceneManager.LoadScene(0);
    }
}
