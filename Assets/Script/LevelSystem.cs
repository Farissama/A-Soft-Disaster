using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class LevelSystem : MonoBehaviour
{
    public GameObject[] levels;
    public GameObject winPage;
    public GameObject donePage; // <-- Halaman Akhir (Congratulations)
    public UnityEvent onSemuaLevelSelesai; // <-- Event Pas Game Tamat!

    private int currentLevel = 0;
    private static bool isTestingRestart = false; // Track restart saat testing di Unity Editor

    void Start()
    {
        int levelToLoad = GameManager.Instance.selectedTheme;

        if (GameManager.Instance != null && GameManager.Instance.isRestarting)
        {
            // Main dari Main Menu lalu Restart
            levelToLoad = PlayerPrefs.GetInt("CurrentLevel", 0);
            GameManager.Instance.isRestarting = false;
        }
        else if (GameManager.Instance != null && !GameManager.Instance.isRestarting)
        {
            // Main Baru dari Main Menu
            levelToLoad = GameManager.Instance.selectedTheme;
        }
        else if (isTestingRestart == true)
        {
            // Restart saat ngetest LANGSUNG dari scene Puzzle (tanpa Main Menu)
            levelToLoad = PlayerPrefs.GetInt("CurrentLevel", 0);
            isTestingRestart = false;
        }
        else
        {
            // Awal Mula ngetest LANGSUNG dari scene Puzzle
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null && levels[i].activeSelf)
                {
                    levelToLoad = i; 
                    break;
                }
            }
        }
        
        ShowLevel(levelToLoad);
        if (winPage != null) winPage.SetActive(false);
        if (donePage != null) donePage.SetActive(false); // Pastikan hidden
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
        if (winPage != null) winPage.SetActive(true);
    }

    public void NextLevel()
    {
        if (winPage != null) winPage.SetActive(false);
        GameManager.Instance.selectedTheme++;

        if (currentLevel + 1 < levels.Length)
        {
            ShowLevel(currentLevel + 1);
        }
        else
        {
            // Semua level sudah selesai! Tampilkan Done Page!
            if (donePage != null)
            {
                // Agar tidak tersangkut di level terakhir kalau player tutup aplikasinya:
                PlayerPrefs.DeleteKey("CurrentLevel"); 
                onSemuaLevelSelesai?.Invoke(); // Trigger Animasi Credit Scene!
                donePage.SetActive(true);
            }
            else
            {
                // Kalau done page belum di set, paksa ke Main Menu
                if (GameManager.Instance != null)
                    GameManager.Instance.isRestarting = false;
                    
                PlayerPrefs.DeleteKey("CurrentLevel");
                SceneManager.LoadScene("Main Menu"); 
            }
        }
    }

    public void RestartLevelPaksakanKeLevelSatu()
    {
        // Khusus dari Done Page jika user mau MENGULANG GAME dari Level 1!
        PlayerPrefs.SetInt("CurrentLevel", 0);
        if (GameManager.Instance != null)
            GameManager.Instance.isRestarting = true;
        GameManager.Instance.selectedTheme = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RestartLevel()
    {
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        
        if (GameManager.Instance != null)
            GameManager.Instance.isRestarting = true;
        else
            isTestingRestart = true;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.isRestarting = false;
            
        SceneManager.LoadScene(0); // atau scene Main Menu
    }
}
