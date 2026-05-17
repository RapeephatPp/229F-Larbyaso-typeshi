using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private CanvasGroup mainMenuPanel;
    [SerializeField] private CanvasGroup optionsPanel;
    [SerializeField] private CanvasGroup creditsPanel;
    [SerializeField] private CanvasGroup levelSelectPanel; // [เพิ่มใหม่] หน้าต่างเลือกด่าน

    [Header("Level Select System")]
    [Tooltip("ใส่ปุ่มเลือกด่านเรียงตามลำดับ (Stage 1, Stage 2, ...)")]
    [SerializeField] private Button[] stageButtons; // [เพิ่มใหม่] อาเรย์เก็บปุ่มด่านต่างๆ

    [Header("Animation Settings")]
    [SerializeField] private float transitionSpeed = 0.3f;
    [SerializeField] private Vector2 slideOffset = new Vector2(500f, 0f);

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

    private CanvasGroup currentPanel;

    void Start()
    {

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerData.ResetData();

        // เช็คว่าเพิ่งจบเกมมาหรือเปล่า (เปิดหน้า Credits)
        if (PlayerPrefs.GetInt("ShowCreditsOnLoad", 0) == 1)
        {
            PlayerPrefs.SetInt("ShowCreditsOnLoad", 0);
            PlayerPrefs.Save();

            currentPanel = creditsPanel;
            ShowPanelImmediate(creditsPanel);
            HidePanelImmediate(mainMenuPanel);
            HidePanelImmediate(optionsPanel);
            HidePanelImmediate(levelSelectPanel);
        }
        else
        {
            currentPanel = mainMenuPanel;
            ShowPanelImmediate(mainMenuPanel);
            HidePanelImmediate(optionsPanel);
            HidePanelImmediate(creditsPanel);
            HidePanelImmediate(levelSelectPanel);
        }

        LoadSettings();
        UpdateStageUnlocks(); // เช็คว่าปลดล็อกด่านไหนบ้าง
    }

    // ==========================================
    // MENU NAVIGATION 
    // ==========================================
    public void OpenLevelSelect() { SwitchPanel(levelSelectPanel); } // เปลี่ยนให้กด Play แล้วมาหน้าเลือกด่าน
    public void OpenOptions() { SwitchPanel(optionsPanel); }
    public void OpenCredits() { SwitchPanel(creditsPanel); }
    public void BackToMainMenu() { SwitchPanel(mainMenuPanel); }

    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }

    // ==========================================
    // LEVEL SELECTOR LOGIC (โหลดด่าน)
    // ==========================================
    public void LoadStage(string sceneName)
    {
        // รีเซ็ตเวลา Speedrun กลับเป็น 0 เมื่อเริ่มด่านใหม่จากเมนู
        GameManager.sessionTime = 0f;
        GameManager.isSessionTimerActive = true;

        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene(sceneName); 
        else
            SceneManager.LoadScene(sceneName); 
    }

    private void UpdateStageUnlocks()
    {
        // ดึงค่าการผ่านด่านจากเครื่อง (ถ้าเพิ่งเล่นครั้งแรก ค่าจะเป็น 1 คือเล่นได้แค่ด่านแรก)
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] != null)
            {
                // ถ้าด่านนั้นๆ มีลำดับ (i+1) น้อยกว่าหรือเท่ากับด่านที่ปลดล็อก ให้ปุ่มกดได้
                stageButtons[i].interactable = (i + 1) <= unlockedLevel;
            }
        }
    }

    // ระบบปลดล็อกด่านทั้งหมด (เผื่อเอาไว้ใช้ตอนทำปุ่ม Dev Cheat)
    public void UnlockAllStages()
    {
        PlayerPrefs.SetInt("UnlockedLevel", 999);
        PlayerPrefs.Save();
        UpdateStageUnlocks();
    }

    // ==========================================
    // SETTINGS LOGIC 
    // ==========================================
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
        AudioListener.volume = masterVolSlider.value;
    }

    private void LoadSettings()
    {
        if (masterVolSlider == null) return;
        masterVolSlider.value = PlayerPrefs.GetFloat("MasterVol", 0.5f);
        musicVolSlider.value = PlayerPrefs.GetFloat("MusicVol", 0.5f);
        vfxVolSlider.value = PlayerPrefs.GetFloat("VFXVol", 0.5f);
        fovSlider.value = PlayerPrefs.GetFloat("FOV", 60f); 
        sensSlider.value = PlayerPrefs.GetFloat("Sensitivity", 200f);
        headBobToggle.isOn = PlayerPrefs.GetInt("HeadBob", 1) == 1;
        screenShakeToggle.isOn = PlayerPrefs.GetInt("ScreenShake", 1) == 1;
        UpdateValueTexts(); 
    }

    public void ResetSettings()
    {
        masterVolSlider.value = 0.5f;
        musicVolSlider.value = 0.5f;
        vfxVolSlider.value = 0.5f;
        fovSlider.value = 60f;
        sensSlider.value = 300f;
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

    // ==========================================
    // UI ANIMATIONS (Slide & Fade)
    // ==========================================
    private void SwitchPanel(CanvasGroup targetPanel)
    {
        if (currentPanel == targetPanel) return;
        StartCoroutine(AnimateTransition(currentPanel, targetPanel));
        currentPanel = targetPanel;
    }

    private IEnumerator AnimateTransition(CanvasGroup hidePanel, CanvasGroup showPanel)
    {
        showPanel.gameObject.SetActive(true);
        showPanel.alpha = 0f;
        showPanel.blocksRaycasts = false;
        
        RectTransform hideRect = hidePanel.GetComponent<RectTransform>();
        RectTransform showRect = showPanel.GetComponent<RectTransform>();

        showRect.anchoredPosition = slideOffset;
        Vector2 hideTargetPos = -slideOffset; 

        float time = 0f;
        while (time < transitionSpeed)
        {
            time += Time.unscaledDeltaTime; 
            float t = time / transitionSpeed;
            t = t * t * (3f - 2f * t); 

            hidePanel.alpha = Mathf.Lerp(1f, 0f, t);
            showPanel.alpha = Mathf.Lerp(0f, 1f, t);

            hideRect.anchoredPosition = Vector2.Lerp(Vector2.zero, hideTargetPos, t);
            showRect.anchoredPosition = Vector2.Lerp(slideOffset, Vector2.zero, t);

            yield return null;
        }

        HidePanelImmediate(hidePanel);
        ShowPanelImmediate(showPanel);
        hideRect.anchoredPosition = Vector2.zero; 
    }

    private void HidePanelImmediate(CanvasGroup panel)
    {
        panel.alpha = 0f;
        panel.blocksRaycasts = false;
        panel.interactable = false;
        panel.gameObject.SetActive(false);
    }

    private void ShowPanelImmediate(CanvasGroup panel)
    {
        panel.alpha = 1f;
        panel.blocksRaycasts = true;
        panel.interactable = true;
        panel.gameObject.SetActive(true);
        panel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }
}