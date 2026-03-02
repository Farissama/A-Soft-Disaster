using UnityEngine;
using UnityEngine.SceneManagement;

public class ThemeButton : MonoBehaviour
{
    public int themeID; // isi 0-4

    public void SelectTheme()
    {
        GameManager.Instance.selectedTheme = themeID;
        SceneManager.LoadScene("Puzzle");
    }
}
