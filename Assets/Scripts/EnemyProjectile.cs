using UnityEngine;

/// <summary>
/// กระสุนที่ศัตรูระยะไกล (RangedEnemyAI) ยิงออกมา
/// - บินตรงไปในทิศทางที่กำหนด
/// - ชนผู้เล่น → สร้างดาเมจ + ผลักกระเด็น
/// - ชนอย่างอื่น หรือหมดเวลา → ทำลายตัวเอง
/// 
/// วิธีใช้:
/// 1. สร้าง Empty GameObject → เพิ่ม Script นี้
/// 2. เพิ่ม SphereCollider (Is Trigger = true, Radius ~0.2)
/// 3. เพิ่ม Rigidbody (Use Gravity = false, Is Kinematic = true)
/// 4. (ตัวเลือก) เพิ่ม LineRenderer หรือ TrailRenderer สำหรับ Visual
/// 5. Save เป็น Prefab แล้วลากไปใส่ช่อง projectilePrefab ของ RangedEnemyAI
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Stats")]
    [Tooltip("ความเร็วกระสุน (หน่วย/วินาที)")]
    [SerializeField] private float speed = 15f;

    [Tooltip("ดาเมจที่สร้างให้ผู้เล่น")]
    [SerializeField] private float damage = 15f;

    [Tooltip("แรงผลักผู้เล่นเมื่อโดนกระสุน")]
    [SerializeField] private float knockbackForce = 10f;

    [Tooltip("เวลา (วินาที) ก่อนทำลายตัวเองถ้าไม่ชนอะไร")]
    [SerializeField] private float lifetime = 5f;

    [Header("Visual")]
    [Tooltip("ขนาดกระสุน (Scale ตัว Renderer)")]
    [SerializeField] private float projectileScale = 0.3f;

    [Tooltip("สีกระสุน")]
    [SerializeField] private Color projectileColor = new Color(1f, 0.3f, 0.1f, 1f); // สีส้มแดง

    // ทิศทางที่กระสุนจะบินไป (ถูก set โดย RangedEnemyAI)
    private Vector3 direction;
    private bool isInitialized = false;

    /// <summary>
    /// เรียกโดย RangedEnemyAI เพื่อกำหนดทิศทางการบิน
    /// </summary>
    public void Initialize(Vector3 fireDirection)
    {
        direction = fireDirection.normalized;
        isInitialized = true;

        // หมุนกระสุนให้หันหน้าตามทิศที่บิน
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // ตั้ง Scale
        transform.localScale = Vector3.one * projectileScale;

        // สร้าง Visual อัตโนมัติถ้ายังไม่มี Renderer
        SetupVisual();

        // ทำลายตัวเองหลังหมดเวลา
        Destroy(gameObject, lifetime);
    }

    private void SetupVisual()
    {
        // ถ้ามี Renderer อยู่แล้ว (จาก Prefab) ให้ตั้งสี
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = projectileColor;
            return;
        }

        // ถ้ายังไม่มี Visual → สร้าง Sphere mesh อัตโนมัติ
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();

        // ใช้ Sphere mesh จาก Unity primitive
        GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mf.mesh = tempSphere.GetComponent<MeshFilter>().mesh;
        Destroy(tempSphere);

        // สร้าง Material เรืองแสง
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = projectileColor;
        mr.material = mat;

        // เพิ่ม Trail สวยๆ
        TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = projectileScale * 0.5f;
        trail.endWidth = 0f;
        trail.material = mat;
        trail.startColor = projectileColor;
        trail.endColor = new Color(projectileColor.r, projectileColor.g, projectileColor.b, 0f);
    }

    private void Update()
    {
        if (!isInitialized) return;

        // เคลื่อนที่ตรงไปข้างหน้า
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // ข้ามการชนกับศัตรูตัวอื่น (ไม่ให้กระสุนศัตรูยิงศัตรูกันเอง)
        if (other.GetComponent<EnemyHealth>() != null) return;
        if (other.GetComponent<RangedEnemyAI>() != null) return;
        if (other.GetComponent<EnemyAI>() != null) return;

        // เช็คว่าชนผู้เล่นหรือไม่
        PlayerHealth playerHP = other.GetComponent<PlayerHealth>();
        if (playerHP != null)
        {
            // สร้างดาเมจ
            playerHP.TakeDamage(damage);

            // ผลักผู้เล่นกระเด็น
            KnockbackReceiver knockback = other.GetComponent<KnockbackReceiver>();
            if (knockback != null)
            {
                knockback.AddImpact(direction, knockbackForce);
            }

            Destroy(gameObject);
            return;
        }

        // ชนอย่างอื่น (กำแพง, พื้น ฯลฯ) → ทำลาย
        // เช็คว่าไม่ใช่ Trigger collider อื่น (เช่น Point Orb, PickupZone)
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
