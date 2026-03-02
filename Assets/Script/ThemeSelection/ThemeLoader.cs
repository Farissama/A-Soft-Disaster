using UnityEngine;

public class ThemeLoader : MonoBehaviour
{
    public GameObject[] themes;

    void Start()
    {
        int id = GameManager.Instance.selectedTheme;

        for (int i = 0; i < themes.Length; i++)
        {
            themes[i].SetActive(i == id);
        }
    }
}
