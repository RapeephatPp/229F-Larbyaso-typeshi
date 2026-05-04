using System.Collections;
using UnityEngine;


public class PointOrb : MonoBehaviour
{
    // ==============================
    
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
    
    // ==============================
    [Header("Collect Effect")]
    [Tooltip("เสียงตอนเก็บ Orb")]
    public AudioClip collectSound;

    [Tooltip("Particle ตอนเก็บ Orb (ไม่บังคับ)")]
    public ParticleSystem collectParticle;

    // ==============================
    
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
        
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;

        isCollected = true;
        StartCoroutine(CollectRoutine());
    }

    private IEnumerator CollectRoutine()
    {
        
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddOrbScore(pointValue);
        }
        else
        {
            Debug.LogWarning("[PointOrb] ไม่พบ ScoreManager ในฉาก! ตรวจสอบว่ามี GameObject ที่ติด ScoreManager แล้ว DontDestroyOnLoad หรือไม่");
        }

        
        if (collectParticle != null)
        {
            collectParticle.transform.SetParent(null);
            collectParticle.Play();
            Destroy(collectParticle.gameObject, collectParticle.main.duration + 1f);
        }

        
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;

        yield return new WaitForSeconds(0.1f);

        Destroy(gameObject);
    }

    
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.4f); 
        Gizmos.DrawSphere(transform.position, 0.6f);
        Gizmos.color = new Color(1f, 0.9f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }
}
