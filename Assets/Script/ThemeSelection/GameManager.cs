using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int selectedTheme = 0; // 0-4 untuk tema 1-5
    public bool isRestarting = false;
    public int currentLevel = 1;

    private void Awake()
    {
        if (Instance == null)
        {
            // PENTING: DontDestroyOnLoad HANYA BERFUNGSI pada objek root (tanpa Parent).
            // Karena GameManager ini menempel di UI `BackgroundTheme` (punya parent Canvas), 
            // maka kita harus memindahkannya ke Object baru di luar Canvas agar tidak hancur.
            if (transform.parent != null)
            {
                // 1. Buat Objek Kosong Baru di paling luar (Root)
                GameObject persistentObj = new GameObject("PersistentGameManager");
                
                // 2. Tambahkan Script GameManager ini ke dalam Objek baru tersebut
                Instance = persistentObj.AddComponent<GameManager>();
                
                // 3. Salin data pemilihan level agar tidak hilang
                Instance.selectedTheme = this.selectedTheme;
                Instance.isRestarting = this.isRestarting;
                
                // 4. Hapus SCRIPT ini dari UI Bungkusan lama 
                // (Gunakan Destroy(this) BUKAN Destroy(gameObject) agar gambarnya tetap ada!)
                Destroy(this);
            }
            else
            {
                // Jika sudah berada di objek tanpa parent
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (Instance != this)
        {
            // Jika kita kembali dari scene Puzzle ke Main Menu dan Script GameManager lama (baru load) muncul lagi, hapus SCRIPT-nya saja.
            Destroy(this);
        }
    }

    private void Start()
    {
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
    }
}
