using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject gameClearPanel;
    
    [Header("Game Clear UI")]
    [SerializeField] private TextMeshProUGUI clearTimeText;
    [SerializeField] private TextMeshProUGUI clearBestTimeText;
    
    [Header("Game Clear UI - Score")]
    [Tooltip("Text แสดงคะแนนจาก Orb เช่น ORB SCORE: 500")]
    [SerializeField] private TextMeshProUGUI clearOrbScoreText;
    [Tooltip("Text แสดง Time Bonus เช่น TIME BONUS: +3200")]
    [SerializeField] private TextMeshProUGUI clearTimeBonusText;
    [Tooltip("Text แสดงคะแนนรวม เช่น FINAL SCORE: 3700")]
    [SerializeField] private TextMeshProUGUI clearFinalScoreText;
    [Tooltip("Text แสดง Best Score เช่น BEST SCORE: 3700")]
    [SerializeField] private TextMeshProUGUI clearBestScoreText;

    [Header("Level Information")]
    [HideInInspector] public string currentLevelName;
    [HideInInspector] public float bestTime;
    
    public static float sessionTime = 0f;
    public static bool isSessionTimerActive = true;
    
    public float currentTime { get { return sessionTime; } }
    public bool timerActive { get { return isSessionTimerActive; } }

    [Header("Options Sliders")]
    [SerializeField] private Slider masterVolSlider;
    [SerializeField] private Slider musicVolSlider;
    [SerializeField] private Slider vfxVolSlider;
    [SerializeField] private Slider fovSlider;
    [SerializeField] private Slider sensSlider;
    [SerializeField] private Toggle headBobToggle;
    [SerializeField] private Toggle screenShakeToggle;

    [Header("Options Value Texts")]
    [SerializeField] private TextMeshProUGUI masterVolText;
    [SerializeField] private TextMeshProUGUI musicVolText;
    [SerializeField] private TextMeshProUGUI vfxVolText;
    [SerializeField] private TextMeshProUGUI fovText;
    [SerializeField] private TextMeshProUGUI sensText;

    [Header("Audio Settings")]
    [Tooltip("ใส่ AudioSource ที่ใช้เล่นเพลงแบคกราวน์ของเกม")]
    public AudioSource bgmSource;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    [HideInInspector] public bool isPaused = false;
    [HideInInspector] public bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (masterVolSlider != null) masterVolSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (musicVolSlider != null) musicVolSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (vfxVolSlider != null) vfxVolSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (fovSlider != null) fovSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (sensSlider != null) sensSlider.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (headBobToggle != null) headBobToggle.onValueChanged.AddListener(delegate { ApplySettings(); });
        if (screenShakeToggle != null) screenShakeToggle.onValueChanged.AddListener(delegate { ApplySettings(); });

        LoadSettingsUI(); 

        currentLevelName = SceneManager.GetActiveScene().name;
        bestTime = PlayerPrefs.GetFloat("BestTime_" + currentLevelName, 0f); 
        
        // โหลด Game Clear Panel จาก Resources อัตโนมัติ (ถ้ายังไม่ได้ assign ใน Inspector)
        InitGameClearPanel();
    }

    private void Update()
    {
        if (isSessionTimerActive && !isPaused && !isGameOver)
        {
            sessionTime += Time.deltaTime; 
        }

        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) && !isGameOver)
        {
            if (isPaused)
            {
                if (optionsPanel != null && optionsPanel.activeSelf) CloseOptions();
                else ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    
    public void LevelComplete()
    {
        if (bestTime == 0f || sessionTime < bestTime)
        {
            bestTime = sessionTime;
            PlayerPrefs.SetFloat("BestTime_" + currentLevelName, bestTime);
            PlayerPrefs.Save();
            Debug.Log("New Best Time for " + currentLevelName + ": " + bestTime);
        }
    }

    // ==============================
    // AUTO-INIT GAME CLEAR PANEL จาก Resources
    // ทำงานอัตโนมัติทุก Stage ไม่ต้องตั้งค่าใน Inspector
    // ==============================
    private void InitGameClearPanel()
    {
        // ถ้า assign ใน Inspector แล้ว ข้ามไปได้เลย
        if (gameClearPanel != null)
        {
            WireButtonEvents(gameClearPanel);
            return;
        }

        // โหลด Prefab จาก Resources/Prefabs/
        GameObject prefab = Resources.Load<GameObject>("Prefabs/GameClearPanel");
        if (prefab == null)
        {
            Debug.LogError("[GameManager] ไม่พบ GameClearPanel Prefab!\n" +
                           "กรุณารันเมนู: Tools → Score System → Save Game Clear Panel as Prefab ก่อน");
            return;
        }

        // หา Canvas ในฉาก (ถ้าไม่มีสร้างใหม่)
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // EventSystem (ถ้าไม่มีปุ่มกดไม่ได้)
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // Instantiate Panel
        gameClearPanel = Instantiate(prefab, canvas.transform);
        gameClearPanel.SetActive(false);

        // หา Text references จากชื่อ Child Object
        Transform card = gameClearPanel.transform.Find("CardBackground");
        if (card == null)
        {
            Debug.LogError("[GameManager] ไม่พบ CardBackground ใน GameClearPanel Prefab!");
            return;
        }

        clearTimeText       = card.Find("ClearTimeText")?.GetComponent<TextMeshProUGUI>();
        clearBestTimeText   = card.Find("ClearBestTimeText")?.GetComponent<TextMeshProUGUI>();
        clearOrbScoreText   = card.Find("OrbScoreText")?.GetComponent<TextMeshProUGUI>();
        clearTimeBonusText  = card.Find("TimeBonusText")?.GetComponent<TextMeshProUGUI>();
        clearFinalScoreText = card.Find("FinalScoreText")?.GetComponent<TextMeshProUGUI>();
        clearBestScoreText  = card.Find("BestScoreText")?.GetComponent<TextMeshProUGUI>();

        // เชื่อมปุ่ม
        WireButtonEvents(gameClearPanel);

        Debug.Log("[GameManager] ✅ โหลด GameClearPanel สำเร็จ! (Auto-loaded from Resources)");
    }

    // เชื่อมปุ่มใน Panel กับฟังก์ชันของ GameManager
    private void WireButtonEvents(GameObject panel)
    {
        Transform card = panel.transform.Find("CardBackground");
        if (card == null) return;

        Button restartBtn  = card.Find("RestartButton")?.GetComponent<Button>();
        Button mainMenuBtn = card.Find("MainMenuButton")?.GetComponent<Button>();

        if (restartBtn != null)
        {
            restartBtn.onClick.RemoveAllListeners();
            restartBtn.onClick.AddListener(RestartGame);
        }
        if (mainMenuBtn != null)
        {
            mainMenuBtn.onClick.RemoveAllListeners();
            mainMenuBtn.onClick.AddListener(LoadMainMenu);
        }
    }

    // ==============================
    // PAUSE & GAME OVER & OPTIONS
    // ==============================
    public void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        Time.timeScale = 1f; 
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void GameOver()
    {
        isGameOver = true;
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        
        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene(SceneManager.GetActiveScene().name); 
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        
        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene("MainMenu"); 
        else
            SceneManager.LoadScene("MainMenu"); 
    }

    public void OpenOptions()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
        LoadSettingsUI(); 
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void ApplySettings()
    {
        PlayerPrefs.SetFloat("MasterVol", masterVolSlider.value);
        PlayerPrefs.SetFloat("MusicVol", musicVolSlider.value);
        PlayerPrefs.SetFloat("VFXVol", vfxVolSlider.value);
        PlayerPrefs.SetFloat("FOV", fovSlider.value);
        PlayerPrefs.SetFloat("Sensitivity", sensSlider.value);
        PlayerPrefs.SetInt("HeadBob", headBobToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("ScreenShake", screenShakeToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();

        UpdateValueTexts(); 

        if (playerMovement != null)
        {
            playerMovement.ApplySettingsFromSave();
            
            // ให้ปืนอัปเดตความดังด้วยเผื่อผู้เล่นปรับสไลเดอร์
            Shotgun shotgun = playerMovement.GetComponentInChildren<Shotgun>();
            if (shotgun != null) shotgun.ApplySettingsFromSave();
        }
        
        AudioListener.volume = masterVolSlider.value;
        
        if (bgmSource != null)
        {
            bgmSource.volume = musicVolSlider.value;
        }
        
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy.monsterAudioSource != null)
            {
                enemy.monsterAudioSource.volume = vfxVolSlider.value;
            }
        }
    }

    private void LoadSettingsUI()
    {
        if (masterVolSlider == null) return; 
        masterVolSlider.value = PlayerPrefs.GetFloat("MasterVol", 1f);
        musicVolSlider.value = PlayerPrefs.GetFloat("MusicVol", 1f);
        vfxVolSlider.value = PlayerPrefs.GetFloat("VFXVol", 1f);
        fovSlider.value = PlayerPrefs.GetFloat("FOV", 60f); 
        sensSlider.value = PlayerPrefs.GetFloat("Sensitivity", 900f);
        headBobToggle.isOn = PlayerPrefs.GetInt("HeadBob", 1) == 1;
        screenShakeToggle.isOn = PlayerPrefs.GetInt("ScreenShake", 1) == 1;
        
        if (bgmSource != null)
        {
            bgmSource.volume = musicVolSlider.value;
        }

        AudioListener.volume = masterVolSlider.value;

        UpdateValueTexts();
    }

    public void ResetSettings()
    {
        masterVolSlider.value = 0.5f;
        musicVolSlider.value = 0.5f;
        vfxVolSlider.value = 0.5f;
        fovSlider.value = 60f;
        sensSlider.value = 900f;
        headBobToggle.isOn = true;
        screenShakeToggle.isOn = true;
        
        ApplySettings();
    }
    
    private void UpdateValueTexts()
    {
        if (masterVolText != null) masterVolText.text = Mathf.RoundToInt(masterVolSlider.value * 100) + "%";
        if (musicVolText != null) musicVolText.text = Mathf.RoundToInt(musicVolSlider.value * 100) + "%";
        if (vfxVolText != null) vfxVolText.text = Mathf.RoundToInt(vfxVolSlider.value * 100) + "%";
        if (fovText != null) fovText.text = Mathf.RoundToInt(fovSlider.value).ToString();
        if (sensText != null) sensText.text = Mathf.RoundToInt(sensSlider.value).ToString();
    }
    
    // ==============================
    // GAME CLEAR (จบเกมสมบูรณ์)
    // ==============================
    public void GameClear()
    {
        isSessionTimerActive = false; // หยุดเวลา Speedrun

        // ==============================
        // คำนวณ Best Time
        // ==============================
        float fullGameBest = PlayerPrefs.GetFloat("FullGameBestTime", 0f);
        if (fullGameBest == 0f || sessionTime < fullGameBest)
        {
            fullGameBest = sessionTime;
            PlayerPrefs.SetFloat("FullGameBestTime", fullGameBest);
            PlayerPrefs.Save();
        }

        // ==============================
        // คำนวณ Score ผ่าน ScoreManager
        // ==============================
        int orbScore = 0;
        int timeBonus = 0;
        int finalScore = 0;
        int bestScore = 0;

        if (ScoreManager.Instance != null)
        {
            finalScore = ScoreManager.Instance.CalculateFinalScore(sessionTime, currentLevelName);
            orbScore   = ScoreManager.Instance.orbScore;
            timeBonus  = ScoreManager.Instance.timeBonus;
            bestScore  = ScoreManager.Instance.GetBestScore(currentLevelName);
        }

        // ==============================
        // แสดงผลลงบน UI — เวลา
        // ==============================
        if (clearTimeText != null)
            clearTimeText.text = "YOUR TIME: " + FormatTime(sessionTime);
        if (clearBestTimeText != null)
            clearBestTimeText.text = "BEST TIME: " + FormatTime(fullGameBest);

        // ==============================
        // แสดงผลลงบน UI — Score
        // ==============================
        if (clearOrbScoreText != null)
            clearOrbScoreText.text = "ORB SCORE: " + orbScore.ToString("N0");
        if (clearTimeBonusText != null)
            clearTimeBonusText.text = "TIME BONUS: +" + timeBonus.ToString("N0");
        if (clearFinalScoreText != null)
            clearFinalScoreText.text = "FINAL SCORE: " + finalScore.ToString("N0");
        if (clearBestScoreText != null)
            clearBestScoreText.text = "BEST SCORE: " + bestScore.ToString("N0");

        // เปิดหน้าต่างจบเกม และหยุดเวลา
        gameClearPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ReturnToMainMenuCredits()
    {
        Time.timeScale = 1f;
        
        // เซฟ "ธง" ไว้บอกหน้า Main Menu ให้เปิดหน้า Credits ทันที
        PlayerPrefs.SetInt("ShowCreditsOnLoad", 1);
        PlayerPrefs.Save();

        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }

    // ฟังก์ชันช่วยจัดรูปแบบเวลา 00:00.00
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }
    
}