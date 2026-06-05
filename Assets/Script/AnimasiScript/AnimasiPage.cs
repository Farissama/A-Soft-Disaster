using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AnimasiPage : MonoBehaviour
{
    [Header("=== General Settings ===")]
    public float screenBounceDuration = 1.2f;
    public LeanTweenType moveEaseType = LeanTweenType.easeInOutBack;

    [Header("=== 1. Start Game Animation ===")]
    public bool playStartAnimOnAwake = false;
    public GameObject startBlackScreen; // Masukkan panel/gambar warna hitam penuh
    public GameObject startLightObject; // Masukkan gambar lampu + cahayanya
    public GameObject lampu;
    public float lightFadeInDuration = 1f;
    public float waitBeforeBlackScreenFade = 1f;
    public float blackScreenFadeDuration = 1.5f;

    [Header("=== 2. Play To Theme Transition ===")]
    public GameObject mainMenuPage;     // Objek Canvas Main Menu
    public GameObject selectThemePage;  // Objek Canvas Select Theme
    public GameObject mainMenuFadingObject; // Opsional: objek di main menu yg mau memudar
    public GameObject selectThemeFadingObject; // Opsional: objek di theme yg perlahan muncul
    public UnityEvent onPlayTransitionComplete;

    [Header("=== 3. Theme To Level / Puzzle Buttons ===")]
    public GameObject loadingScreenObjectMasuk; // Layar loading yang akan muncul menutup layar
    public float loadingScreenMunculDuration = 0.5f;
    public float tungguLoadingLaluPindahScene = 2f; 
    
    [Header("   ↳ Event ketika Loading Tertutup Penuh:")]
    public UnityEvent onPindahDariThemeKeLevel; // Untuk tombol di peta
    public UnityEvent onKembaliKeMainMenu;      // Untuk tombol MainMenu / Exit
    public UnityEvent onRestartLevelSekarang;   // Untuk tombol Replay / Reset BIASA
    public UnityEvent onRestartDariDonePage;    // KHUSUS untuk tombol Replay di DonePage (Ke Level 1)

    [Header("=== 4. Masuk Ke Level (Loading 2: Awal Scene Puzzle) ===")]
    public bool mainkanDiAwalScenePuzzle = false;
    public GameObject loadingScreenObjectPudar; // Layar loading yang siap memudar di awal scene
    public float loadingFadeOutDuration = 1f;
    public GameObject activeLevelPage; // Halaman level yang akan bergeser ke atas
    public UnityEvent onPudarSelesaiEvent;

    [Header("=== 5. Setting Animation ===")]
    public GameObject settingPage; // Halaman Setting
    public float settingAnimDuration = 0.8f;
    public UnityEvent onSettingOpened;
    public UnityEvent onSettingClosed;

    [Header("=== 6. Win Page To Next Level ===")]
    public GameObject winPageObject; // Halaman Win
    public float winToLoadingFadeInDuration = 1f;  // durasi loading memudar menutup layar
    public float winLoadingStayDuration = 1f;      // durasi loading diam mem-block layar (terjadi pergantian level)
    public float winLoadingFadeOutDuration = 1f;   // durasi loading memudar kembali menghilang
    public UnityEvent onMulaiTransisiBerikutnya; // Event pas mulai loading penuh

    private float _screenHeight;

    private void Start()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null && mainMenuPage != null) 
        {
            parentCanvas = mainMenuPage.GetComponentInParent<Canvas>();
        }

        if (parentCanvas != null)
        {
            _screenHeight = parentCanvas.GetComponent<RectTransform>().rect.height;
        }
        else
        {
            _screenHeight = Screen.height;
        }

        if (_screenHeight < 100) _screenHeight = 1080f;

        if (playStartAnimOnAwake)
        {
            StartCoroutine(StartGameRoutine());
        }

        if (mainkanDiAwalScenePuzzle)
        {
            StartCoroutine(LevelMunculDariLoadingRoutine());
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null) return null;
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = obj.AddComponent<CanvasGroup>();
        }
        return cg;
    }

    // Tambahan: Agar bisa mengingat jika baru buka game atau sudah dari puzzle!
    public static bool sudahPernahMulai = false;

    // ==========================================
    // 1. START GAME ANIMATION
    // ==========================================
    public void PlayStartAnimation()
    {
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        // 🌟 RAHASIA: Membedakan Pertama Kali vs Kembali dari Puzzle!
        // Jika aplikasi dijalankan kurang dari 3 detik yang lalu (Awal Mula Sekali)
        if (Time.time < 3f)
        {
            // === PERTAMA KALI BUKA GAME (Ada Layar Hitam) ===
            if (startBlackScreen != null) startBlackScreen.SetActive(true);
            CanvasGroup blackCG = GetOrAddCanvasGroup(startBlackScreen);
            if (blackCG != null) blackCG.alpha = 1f;
            
            CanvasGroup lightCG = GetOrAddCanvasGroup(startLightObject);
            if (lightCG != null) lightCG.alpha = 0f;

            yield return new WaitForSeconds(1f);

            if (lightCG != null) LeanTween.alphaCanvas(lightCG, 1f, lightFadeInDuration);

            yield return new WaitForSeconds(lightFadeInDuration + waitBeforeBlackScreenFade);
            
            if (blackCG != null)
            {
                LeanTween.alphaCanvas(blackCG, 0f, blackScreenFadeDuration).setOnComplete(() => {
                    if (startLightObject != null) startLightObject.transform.SetAsFirstSibling();
                    startBlackScreen.transform.SetAsFirstSibling();
                    if (lampu != null) lampu.transform.SetSiblingIndex(4);
                    startBlackScreen.SetActive(false);
                });
            }
        }
        else
        {
            // === KEMBALI DARI PUZZLE atau Restart (Hanya Background & Lampu, lalu UI Menyusul) ===
            // Karena game sudah berjalan lebih dari 3 detik!
            
            // 0. (SOLUSI ANTI JLEGG) Kalau ada Layar Loading, hidupkan dulu lalu pudarkan!!
            if (loadingScreenObjectPudar != null)
            {
                loadingScreenObjectPudar.SetActive(true);
                loadingScreenObjectPudar.transform.SetAsLastSibling(); // Majukan paling depan
                CanvasGroup pudarCG = GetOrAddCanvasGroup(loadingScreenObjectPudar);
                pudarCG.alpha = 1f;

                LeanTween.alphaCanvas(pudarCG, 0f, 1f).setOnComplete(() => {
                    loadingScreenObjectPudar.SetActive(false);
                });
            }

            // 1. Matikan blackscreen secara paksa dan pindahkan ke paling belakang
            if (startBlackScreen != null) 
            {
                startBlackScreen.transform.SetAsFirstSibling();
                startBlackScreen.SetActive(false);
            }
            
            // 2. Nyalakan background utama dan lampu SEPENUHNYA
            CanvasGroup lightCG = GetOrAddCanvasGroup(startLightObject);
            if (lightCG != null) lightCG.alpha = 1f;
            if (startLightObject != null)
            {
                startLightObject.transform.SetAsFirstSibling();
                startLightObject.SetActive(true);
            }
            if (lampu != null) lampu.transform.SetSiblingIndex(4);

            // 3. Matikan / Transparansikan tombol-tombol UI awal
            CanvasGroup uiCG = GetOrAddCanvasGroup(mainMenuFadingObject);
            if (uiCG != null) uiCG.alpha = 0f;

            // 4. Kasih jeda 1 detik menikmati lampu
            yield return new WaitForSeconds(waitBeforeBlackScreenFade + 1f);

            // 5. Memudarkan tombol kembali muncul
            if (uiCG != null)
            {
                LeanTween.alphaCanvas(uiCG, 1f, lightFadeInDuration);
            }
        }
    }

    // ==========================================
    // 2. PLAY TO THEME TRANSITION
    // ==========================================
    public void AnimasiPlayToTheme()
    {
        if (mainMenuPage != null && selectThemePage != null)
        {
            selectThemePage.SetActive(true);
            selectThemePage.transform.localPosition = new Vector3(selectThemePage.transform.localPosition.x, -_screenHeight, selectThemePage.transform.localPosition.z);

            LeanTween.moveLocalY(mainMenuPage, _screenHeight, screenBounceDuration).setEase(moveEaseType).setOnComplete(() => {
                mainMenuPage.SetActive(true);
            });

            LeanTween.moveLocalY(selectThemePage, 0f, screenBounceDuration).setEase(moveEaseType).setOnComplete(() => {
                onPlayTransitionComplete?.Invoke();
            });

            CanvasGroup mainCG = GetOrAddCanvasGroup(mainMenuFadingObject);
            CanvasGroup themeCG = GetOrAddCanvasGroup(selectThemeFadingObject);

            if (mainCG != null) LeanTween.alphaCanvas(mainCG, 0f, screenBounceDuration);
            if (themeCG != null)
            {
                themeCG.alpha = 0f;
                LeanTween.alphaCanvas(themeCG, 1f, screenBounceDuration);
            }
        }
    }

    public void CloseTheme()
    {
        if (mainMenuPage != null && selectThemePage != null)
        {
            // Aktifkan main menu dulu
            mainMenuPage.SetActive(true);

            // Pastikan posisi awal main menu di atas (biar turun dengan bounce)
            mainMenuPage.transform.localPosition = new Vector3(
            mainMenuPage.transform.localPosition.x,
            _screenHeight,
            mainMenuPage.transform.localPosition.z
            );

            // Main menu turun ke tengah
            LeanTween.moveLocalY(mainMenuPage, 0f, screenBounceDuration)
            .setEase(moveEaseType);

            // Theme turun ke bawah (kebalikan dari naik tadi)
            LeanTween.moveLocalY(selectThemePage, -_screenHeight, screenBounceDuration)
            .setEase(moveEaseType)
            .setOnComplete(() => {
                selectThemePage.SetActive(false);
                onSettingClosed?.Invoke();
            });

            // Fade dibalik (optional tapi biar konsisten sama OPEN)
            CanvasGroup mainCG = GetOrAddCanvasGroup(mainMenuFadingObject);
            CanvasGroup themeCG = GetOrAddCanvasGroup(selectThemeFadingObject);

            if (mainCG != null)
            {
                mainCG.alpha = 0f;
                LeanTween.alphaCanvas(mainCG, 1f, screenBounceDuration);
            }

            if (themeCG != null)
            {
                LeanTween.alphaCanvas(themeCG, 0f, screenBounceDuration);
            }
        }
    }

    // ==========================================
    // 3. THEME -> LEVEL atau TOMBOL-TOMBOL DI PUZZLE (Memisahkan Event)
    // ==========================================

    
    // Dipanggil oleh tombol Level di Peta
    public void AnimasiDariThemeKeLevel()
    {
        StartCoroutine(LoadingMasukLaluPanggilEvent(onPindahDariThemeKeLevel));
    }

    // Dipanggil oleh tombol Exit / Main Menu (di Pause atau Winpage)
    public void AnimasiKeluarKeMainMenu()
    {
        StartCoroutine(LoadingMasukLaluPanggilEvent(onKembaliKeMainMenu));
    }

    // Dipanggil oleh tombol Reset / Replay (di Pause atau Winpage BIASA)
    public void AnimasiRestartLevel()
    {
        StartCoroutine(LoadingMasukLaluPanggilEvent(onRestartLevelSekarang));
    }

    // Dipanggil oleh tombol Replay khusus di DONE PAGE (Kembali ke Level 1)
    public void AnimasiRestartDariDonePage()
    {
        StartCoroutine(LoadingMasukLaluPanggilEvent(onRestartDariDonePage));
    }

    private IEnumerator LoadingMasukLaluPanggilEvent(UnityEvent eventYangDipanggil)
    {
        CanvasGroup loadCG = GetOrAddCanvasGroup(loadingScreenObjectMasuk);

        if (loadCG != null && loadingScreenObjectMasuk != null)
        {
            loadingScreenObjectMasuk.SetActive(true);
            loadingScreenObjectMasuk.transform.SetAsLastSibling();
            loadCG.alpha = 0f;
            LeanTween.alphaCanvas(loadCG, 1f, loadingScreenMunculDuration);
        }

        yield return new WaitForSeconds(loadingScreenMunculDuration + tungguLoadingLaluPindahScene);

        // Panggil fungsi scene/level sesuai tombol mana yang diklik tadi
        eventYangDipanggil?.Invoke(); 
    }

    // ==========================================
    // 4. AWAL SCENE BARU -> LOADING MEMUDAR -> LEVEL MUNCUL
    // ==========================================
    public void AnimasiLevelMunculDariBawah()
    {
        StartCoroutine(LevelMunculDariLoadingRoutine());
    }

    private IEnumerator LevelMunculDariLoadingRoutine()
    {
        CanvasGroup loadCG = GetOrAddCanvasGroup(loadingScreenObjectPudar);

        // Langsung tampilkan loading menutupi layar di detik 0 (jika dioverride nyala)
        if (loadingScreenObjectPudar != null && loadCG != null)
        {
            loadingScreenObjectPudar.SetActive(true);
            loadCG.alpha = 1f;
            loadingScreenObjectPudar.transform.SetAsLastSibling(); // Paling depan
        }

        // Tunggu sebentar scene settle sebelum memudar 
        yield return new WaitForSeconds(0.5f);

        if (loadCG != null && loadingScreenObjectPudar != null)
        {
            LeanTween.alphaCanvas(loadCG, 0f, loadingFadeOutDuration).setOnComplete(() => {
                loadingScreenObjectPudar.SetActive(false);
                onPudarSelesaiEvent?.Invoke();
            });
        }

        // LEVEL PAGE MUNCUL
        if (activeLevelPage != null)
        {
            activeLevelPage.SetActive(true);
            activeLevelPage.transform.localPosition = new Vector3(activeLevelPage.transform.localPosition.x, -_screenHeight, activeLevelPage.transform.localPosition.z);
            LeanTween.moveLocalY(activeLevelPage, 0f, screenBounceDuration).setEase(moveEaseType);
        }
    }

    // ==========================================
    // 5. SETTING ANIMATION
    // ==========================================
    public void OpenSetting()
    {
        if (settingPage != null)
        {
            settingPage.SetActive(true);
            settingPage.transform.SetAsLastSibling(); // Pastikan setting di paling depan
            settingPage.transform.localPosition = new Vector3(settingPage.transform.localPosition.x, -_screenHeight, settingPage.transform.localPosition.z);
            LeanTween.moveLocalY(settingPage, 0f, settingAnimDuration).setEase(LeanTweenType.easeOutBack).setOnComplete(() => {
                onSettingOpened?.Invoke();
            });
        }
    }

    public void CloseSetting()
    {
        if (settingPage != null)
        {
            LeanTween.moveLocalY(settingPage, -_screenHeight, settingAnimDuration).setEase(LeanTweenType.easeInBack).setOnComplete(() => {
                settingPage.SetActive(false);
                onSettingClosed?.Invoke();
            });
        }
    }

    // ==========================================
    // 6. WIN PAGE -> LOADING -> NEXT LEVEL
    // ==========================================
    public void AnimasiWinMenujuNextLevel()
    {
        StartCoroutine(WinToNextLevelRoutine());
    }

    private IEnumerator WinToNextLevelRoutine()
    {
        CanvasGroup winCG = GetOrAddCanvasGroup(winPageObject);
        CanvasGroup loadCG = GetOrAddCanvasGroup(loadingScreenObjectMasuk);

        if (winCG != null && winPageObject != null)
        {
            LeanTween.alphaCanvas(winCG, 0f, winToLoadingFadeInDuration).setOnComplete(() => {
                winPageObject.SetActive(false);
                winCG.alpha = 1f; // FIX BUG: Reset alpha ke 1 agar muncul di level berikutnya!
            });
        }

        if (loadCG != null && loadingScreenObjectMasuk != null)
        {
            loadingScreenObjectMasuk.SetActive(true);
            loadingScreenObjectMasuk.transform.SetAsLastSibling();
            loadCG.alpha = 0f;
            LeanTween.alphaCanvas(loadCG, 1f, winToLoadingFadeInDuration);
        }

        // 3. TUNGGU DI SINI! Layar loading butuh waktu untuk menutup total
        yield return new WaitForSeconds(winToLoadingFadeInDuration);

        // 4. Barulah memanggil script merubah/mengacak Level berikutnya. (Layar lagi ketutup, aman!)
        onMulaiTransisiBerikutnya?.Invoke();

        // 5. Jeda biarkan pemain lihat layar loadingnya sejenak (biar tidak instan)
        yield return new WaitForSeconds(winLoadingStayDuration);

        // 6. Setelah jeda santai berlalu, barulah pudarkan layarnya menghilang...
        if (loadCG != null && loadingScreenObjectMasuk != null)
        {
            LeanTween.alphaCanvas(loadCG, 0f, winLoadingFadeOutDuration).setOnComplete(() => {
                loadingScreenObjectMasuk.SetActive(false);
            });
        }

        onPudarSelesaiEvent?.Invoke();

        if (activeLevelPage != null)
        {
            activeLevelPage.SetActive(true);
            activeLevelPage.transform.localPosition = new Vector3(activeLevelPage.transform.localPosition.x, -_screenHeight, activeLevelPage.transform.localPosition.z);
            LeanTween.moveLocalY(activeLevelPage, 0f, screenBounceDuration).setEase(moveEaseType);
        }
    }

    // ==========================================
    // 7. PAUSE ANIMATION (Gulung dari Pojok)
    // ==========================================
    [Header("=== 7. Pause Animation ===")]
    [Tooltip("Isi dengan 'pausebg' (Bapak Keseluruhan)")]
    public GameObject pauseLayarGelap; 
    [Tooltip("Isi dengan 'bgpause' (Isi Kertasnya Saja)")]
    public GameObject pauseKertasGulung; 
    public float pauseAnimDuration = 0.5f;

    public void OpenPauseMenu()
    {
        if (pauseLayarGelap != null) pauseLayarGelap.SetActive(true);

        if (pauseKertasGulung != null)
        {
            // Efek akan mulai dari 0 (hilang)
            pauseKertasGulung.transform.localScale = new Vector3(0f, 0f, 1f); 

            // Membesar menjadi 1 full
            LeanTween.scale(pauseKertasGulung, Vector3.one, pauseAnimDuration).setEase(LeanTweenType.easeOutBack);
        }
    }

    public void ClosePauseMenu()
    {
        if (pauseKertasGulung != null)
        {
            // Mengecil kembali menjadi 0
            LeanTween.scale(pauseKertasGulung, new Vector3(0f, 0f, 1f), pauseAnimDuration).setEase(LeanTweenType.easeInBack).setOnComplete(() => {
                // Matikan layar gelapnya hanya JIKA kertas sudah selesai menggulung
                if (pauseLayarGelap != null) pauseLayarGelap.SetActive(false);
            });
        }
        else
        {
            if (pauseLayarGelap != null) pauseLayarGelap.SetActive(false);
        }
    }
    // ==========================================
    // 8. DONE PAGE ANIMATION (Credit Scene berurutan)
    // ==========================================
    [Header("=== 8. Done Page Animation (Berurutan) ===")]
    public GameObject doneBackgroundObject;      // Latar belakang utama (Donepage root)
    public GameObject teksCongratulationsObject; // Tulisan Congratulation (donetext)
    public GameObject teksCreditDanNamaObject;   // Tulisan pembantu pembuat (namecredit dll)
    public GameObject tombolTombolAkhirObject;   // Pembungkus tombol Main Menu & Replay
    public float durasiFadeTiapObjek = 1.5f;     // Berapa lama waktu pudar masing2 objek
    public float jedaAntarPudar = 0.5f;          // Waktu nunggu sebelum objek selanjutnya memudar

    public void AnimasiMunculDonePage()
    {
        StartCoroutine(MunculDonePageRoutine());
    }

    private IEnumerator MunculDonePageRoutine()
    {
        CanvasGroup bgCG = GetOrAddCanvasGroup(doneBackgroundObject);
        CanvasGroup congrCG = GetOrAddCanvasGroup(teksCongratulationsObject);
        CanvasGroup creditCG = GetOrAddCanvasGroup(teksCreditDanNamaObject);
        CanvasGroup btnCG = GetOrAddCanvasGroup(tombolTombolAkhirObject);

        // Menyiapkan semua menjadi transparan terlebih dulu agar tidak nge-flash di layar (jegg!)
        if (bgCG != null) { bgCG.alpha = 0f; if (doneBackgroundObject != null) doneBackgroundObject.SetActive(true); }
        if (congrCG != null) { congrCG.alpha = 0f; if (teksCongratulationsObject != null) teksCongratulationsObject.SetActive(true); }
        if (creditCG != null) { creditCG.alpha = 0f; if (teksCreditDanNamaObject != null) teksCreditDanNamaObject.SetActive(true); }
        if (btnCG != null) { btnCG.alpha = 0f; if (tombolTombolAkhirObject != null) tombolTombolAkhirObject.SetActive(true); }

        // 1. Munculkan Background terlebih dulu
        if (bgCG != null) LeanTween.alphaCanvas(bgCG, 1f, durasiFadeTiapObjek);
        yield return new WaitForSeconds(durasiFadeTiapObjek + jedaAntarPudar);

        // 2. Munculkan tulisan Congratulation
        if (congrCG != null) LeanTween.alphaCanvas(congrCG, 1f, durasiFadeTiapObjek);
        yield return new WaitForSeconds(durasiFadeTiapObjek + jedaAntarPudar);

        // 3. Munculkan tulisan Nama Credit bersaman dengan tombol-tombolnya
        if (creditCG != null) LeanTween.alphaCanvas(creditCG, 1f, durasiFadeTiapObjek);
        if (btnCG != null) LeanTween.alphaCanvas(btnCG, 1f, durasiFadeTiapObjek);
    }
}
