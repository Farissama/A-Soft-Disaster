using UnityEngine;
using UnityEngine.SceneManagement;

public class ThemeButton : MonoBehaviour
{
    public int themeID; // isi 0-4

    // Panggil ini SAAT TOMBOL MAP DITEKAN (Di OnClick yang PERTAMA)
    public void SimpanTemaTerpilih()
    {
        GameManager.Instance.selectedTheme = themeID;
        GameManager.Instance.isRestarting = false; // PENTING: Untuk reset flag restart agar tidak nyangkut!
    }

    // Panggil ini di EVENT "On Pindah Scene Event" milik AnimasiPage
    public void PindahSceneSekarang()
    {
        SceneManager.LoadScene("Puzzle");
    }

    // Fungsi lama bisa kamu abaikan atau hapus agar tidak bingung
    // public void SelectTheme()
    // {
    //     GameManager.Instance.selectedTheme = themeID;
    //     SceneManager.LoadScene("Puzzle");
    // }
}
