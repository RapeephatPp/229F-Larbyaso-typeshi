using UnityEngine;


public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // ==============================
    
    // ==============================
    [HideInInspector] public int orbScore = 0;

    // ==============================
    
    // ==============================
    [Header("Time Bonus Settings")]
    [Tooltip("คะแนน Time Bonus สูงสุด (ถ้าจบเร็วมาก)")]
    public int baseTimeBonus = 5000;

    [Tooltip("ลดคะแนน Time Bonus ต่อวินาทีที่ใช้ไป")]
    public int penaltyPerSecond = 10;

    // ==============================
    
    // ==============================
    [HideInInspector] public int timeBonus = 0;
    [HideInInspector] public int finalScore = 0;

    // ==============================
    
    // ==============================
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==============================
    
    // ==============================
    public void ResetScore()
    {
        orbScore = 0;
        timeBonus = 0;
        finalScore = 0;
    }

    // ==============================
    
    // ==============================
    public void AddOrbScore(int points)
    {
        orbScore += points;
        Debug.Log($"[ScoreManager] +{points} Orb Score! รวม: {orbScore}");
    }

    // ==============================
    
    // ==============================
    public int CalculateFinalScore(float sessionTime, string levelName)
    {
       
        int rawTimeBonus = baseTimeBonus - Mathf.FloorToInt(sessionTime * penaltyPerSecond);
        timeBonus = Mathf.Max(0, rawTimeBonus); // ไม่ต่ำกว่า 0

        
        finalScore = orbScore + timeBonus;

        
        string key = "BestScore_" + levelName;
        int currentBest = PlayerPrefs.GetInt(key, 0);
        if (finalScore > currentBest)
        {
            PlayerPrefs.SetInt(key, finalScore);
            PlayerPrefs.Save();
            Debug.Log($"[ScoreManager] New Best Score for {levelName}: {finalScore}");
        }

        Debug.Log($"[ScoreManager] Final Score = OrbScore({orbScore}) + TimeBonus({timeBonus}) = {finalScore}");
        return finalScore;
    }

    // ==============================
    
    // ==============================
    public int GetBestScore(string levelName)
    {
        return PlayerPrefs.GetInt("BestScore_" + levelName, 0);
    }
}
