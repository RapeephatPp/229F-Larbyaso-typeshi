using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMove : MonoBehaviour
{
    [Header("Settings")]
    public string nextSceneName;
    public bool useBuildIndex = true;

    [Header("Progression")]
    [Tooltip("เมื่อเข้าประตูนี้ จะปลดล็อกด่านเบอร์อะไร (เช่น ผ่านด่าน 1 ให้เซ็ตค่านี้เป็น 2 เพื่อปลดล็อกด่าน 2)")]
    public int unlockNextLevelNumber = 2; // [เพิ่มใหม่]

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerScript = other.GetComponent<PlayerHealth>();
            var shotgunScript = other.GetComponentInChildren<Shotgun>();

            if (playerScript != null)
            {
                PlayerData.SavedHP = playerScript.CurrentHealth;               
                PlayerData.HasData = true;

                Debug.Log($"Saved Data: HP={PlayerData.SavedHP}");
            }
            if (shotgunScript != null)
            {
                PlayerData.SavedTotalAmmo = shotgunScript.TotalAmmo;
                PlayerData.SavedcurrentAmmo = shotgunScript.CurrentAmmo;
                PlayerData.HasData = true;
                Debug.Log($"Saved Ammo: Total={shotgunScript.TotalAmmo}, Current={shotgunScript.CurrentAmmo}");
            }
            GoToNextLevel();
        }
    }

    void GoToNextLevel()
    {
        Debug.Log("Player reached the exit! Loading next level...");

        // --- [เพิ่มใหม่] เซฟการปลดล็อกด่าน ---
        int currentUnlocked = PlayerPrefs.GetInt("UnlockedLevel", 1);
        // ถ้าค่าที่เราต้องการจะปลดล็อก มันสูงกว่าด่านที่ผู้เล่นเคยทำได้ ให้เซฟทับเลย
        if (unlockNextLevelNumber > currentUnlocked)
        {
            PlayerPrefs.SetInt("UnlockedLevel", unlockNextLevelNumber);
            PlayerPrefs.Save();
        }
        // ------------------------------------

        string targetScene = nextSceneName;

        if (useBuildIndex)
        {          
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            string scenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
            targetScene = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        }

        // เรียกใช้งาน SceneFader 
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(targetScene);
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }
}