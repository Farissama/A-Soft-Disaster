using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class VolumeSettings : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider slider;
    public TextMeshProUGUI valueText;
    public string mixerParameter; // MusicVolume / SFXVolume

    [Header("Volume Range")]
    public float minDb = -80f; // mute
    public float maxDb = 5f;   // boost max (bisa diubah di Inspector)

    void Start()
    {
        // Ambil nilai tersimpan (default 100 kalau belum ada)
        float savedValue = PlayerPrefs.GetFloat(mixerParameter, 100f);

        slider.value = savedValue;
        slider.onValueChanged.AddListener(SetVolume);

        SetVolume(slider.value);
    }

    public void SetVolume(float value)
    {
        float dB;

        if (value <= 0)
        {
            dB = minDb;
        }
        else
        {
            // logaritmik + bisa lebih dari 0 dB
            float t = value / 100f;
            dB = Mathf.Lerp(minDb, maxDb, Mathf.Log10(1 + 9 * t));
        }

        audioMixer.SetFloat(mixerParameter, dB);

        // Simpan nilai slider
        PlayerPrefs.SetFloat(mixerParameter, value);

        if (valueText != null)
            valueText.text = value.ToString("0") + "%";
    }
}
