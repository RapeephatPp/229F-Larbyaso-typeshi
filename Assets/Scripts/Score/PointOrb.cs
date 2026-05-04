using System.Collections;
using UnityEngine;

/// <summary>
/// PointOrb — ลูกบอลคะแนนที่วางเป็น Waypoint ในด่าน
/// ผู้เล่นวิ่งผ่าน Trigger แล้วได้คะแนน + เล่น Effect แล้วหายไป
///
/// วิธีใช้:
/// 1. สร้าง GameObject ทรงกลม (Sphere)
/// 2. เพิ่ม Sphere Collider → ติ๊ก "Is Trigger"
/// 3. ลาก Script นี้ใส่
/// 4. ตั้งค่า Point Value, สี, เสียง, Particle ใน Inspector
/// </summary>
public class PointOrb : MonoBehaviour
{
    // ==============================
    // ค่าคะแนนและการมองเห็น
    // ==============================
    [Header("Score Settings")]
    [Tooltip("คะแนนที่ได้เมื่อเก็บ Orb นี้")]
    public int pointValue = 100;

    // ==============================
    // อนิเมชั่น Orb (หมุน + ลอย)
    // ==============================
    [Header("Animation")]
    [Tooltip("ความเร็วในการหมุน (องศา/วินาที)")]
    public float rotateSpeed = 90f;

    [Tooltip("ระยะที่ลอยขึ้นลง")]
    public float bobHeight = 0.3f;

    [Tooltip("ความเร็วในการลอย")]
    public float bobSpeed = 2f;

    // ==============================
    // เสียงและ Effect
    // ==============================
    [Header("Collect Effect")]
    [Tooltip("เสียงตอนเก็บ Orb")]
    public AudioClip collectSound;

    [Tooltip("Particle ตอนเก็บ Orb (ไม่บังคับ)")]
    public ParticleSystem collectParticle;

    // ==============================
    // ตัวแปรภายใน
    // ==============================
    private Vector3 startPosition;
    private bool isCollected = false;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (isCollected) return;

        // หมุนรอบแกน Y
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);

        // ลอยขึ้นลง (Bob)
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        // เช็คว่าเป็นผู้เล่นและยังไม่ถูกเก็บ
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;

        isCollected = true;
        StartCoroutine(CollectRoutine());
    }

    private IEnumerator CollectRoutine()
    {
        // 1. บวกคะแนน
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddOrbScore(pointValue);
        }
        else
        {
            Debug.LogWarning("[PointOrb] ไม่พบ ScoreManager ในฉาก! ตรวจสอบว่ามี GameObject ที่ติด ScoreManager แล้ว DontDestroyOnLoad หรือไม่");
        }

        // 2. เล่น Particle (ถ้ามี) — Detach จาก Orb ก่อนให้ Effect เล่นต่อหลัง Orb หาย
        if (collectParticle != null)
        {
            collectParticle.transform.SetParent(null);
            collectParticle.Play();
            Destroy(collectParticle.gameObject, collectParticle.main.duration + 1f);
        }

        // 3. เล่นเสียง (สร้าง AudioSource ชั่วคราวที่ตำแหน่งนี้)
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        // 4. ซ่อน Orb ทันที แล้วค่อย Destroy
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;

        yield return new WaitForSeconds(0.1f);

        Destroy(gameObject);
    }

    // แสดง Gizmo ใน Scene View ให้มองเห็นตำแหน่ง Orb ง่ายขึ้น
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.4f); // สีเหลืองโปร่งแสง
        Gizmos.DrawSphere(transform.position, 0.6f);
        Gizmos.color = new Color(1f, 0.9f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }
}
