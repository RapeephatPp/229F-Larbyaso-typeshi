using UnityEngine;

/// <summary>
/// Singleton จัดการคะแนนทั้งเกม
/// ติดไว้บน GameObject ที่ DontDestroyOnLoad เพื่อให้คะแนนสะสมข้าม Scene ได้
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // ==============================
    // คะแนนสะสมจาก Orb
    // ==============================
    [HideInInspector] public int orbScore = 0;

    // ==============================
    // ค่าตั้งต้น Time Bonus (ปรับใน Inspector ได้)
    // ==============================
    [Header("Time Bonus Settings")]
    [Tooltip("คะแนน Time Bonus สูงสุด (ถ้าจบเร็วมาก)")]
    public int baseTimeBonus = 5000;

    [Tooltip("ลดคะแนน Time Bonus ต่อวินาทีที่ใช้ไป")]
    public int penaltyPerSecond = 10;

    // ==============================
    // ผลลัพธ์หลังคำนวณ (อ่านได้จากภายนอก)
    // ==============================
    [HideInInspector] public int timeBonus = 0;
    [HideInInspector] public int finalScore = 0;

    // ==============================
    // Awake: ตั้ง Singleton + DontDestroyOnLoad
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
    // รีเซตคะแนนทั้งหมด (เรียกตอนเริ่มเกมใหม่จาก MainMenu)
    // ==============================
    public void ResetScore()
    {
        orbScore = 0;
        timeBonus = 0;
        finalScore = 0;
    }

    // ==============================
    // เพิ่มคะแนนจาก Orb
    // ==============================
    public void AddOrbScore(int points)
    {
        orbScore += points;
        Debug.Log($"[ScoreManager] +{points} Orb Score! รวม: {orbScore}");
    }

    // ==============================
    // คำนวณ Final Score ตอนจบเกม
    // รับ sessionTime จาก GameManager เข้ามา
    // ==============================
    public int CalculateFinalScore(float sessionTime, string levelName)
    {
        // คำนวณ Time Bonus
        int rawTimeBonus = baseTimeBonus - Mathf.FloorToInt(sessionTime * penaltyPerSecond);
        timeBonus = Mathf.Max(0, rawTimeBonus); // ไม่ต่ำกว่า 0

        // รวมคะแนนทั้งหมด
        finalScore = orbScore + timeBonus;

        // บันทึก Best Score รายด่าน
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
    // ดึง Best Score ของด่าน
    // ==============================
    public int GetBestScore(string levelName)
    {
        return PlayerPrefs.GetInt("BestScore_" + levelName, 0);
    }
}
