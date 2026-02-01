using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonUI : MonoBehaviour
{
    [Header("Menu Panels")]
    // Tempat untuk memasukkan BackgroundMain dan BackgroundTheme
    public GameObject backgroundMain;
    public GameObject backgroundTheme;

    [Header("Other Objects")]
    public GameObject pos, detector;

    void Start()
    {
        // Saat game mulai, pastikan Main Menu muncul dan Theme Select sembunyi
        ShowMainMenu();
    }

    void Update()
    {
        
    }
    
    // Fungsi dipanggil saat tombol PLAY ditekan
    public void OpenThemeSelection()
    {
        if (backgroundMain != null) backgroundMain.SetActive(false);   // Matikan Main Menu
        if (backgroundTheme != null) backgroundTheme.SetActive(true);  // Nyalakan Theme UI
    }

    // Fungsi dipanggil saat tombol CLOSE ditekan (di menu tema)
    public void ShowMainMenu()
    {
        if (backgroundMain != null) backgroundMain.SetActive(true);    // Nyalakan Main Menu
        if (backgroundTheme != null) backgroundTheme.SetActive(false); // Matikan Theme UI
    }

    public void LoadToScene(string sceneName)
    {
        Debug.Log(sceneName);
        SceneManager.LoadScene(sceneName);
    }

    public void OneStartClick() // Jika ini untuk pindah Scene lain, biarkan saja
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void OneExitClick()
    {
        if (Application.isPlaying)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
        else
        {
            Application.Quit();
        }
    }
}
