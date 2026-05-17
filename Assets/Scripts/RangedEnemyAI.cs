using UnityEngine;

/// <summary>
/// ศัตรูระยะไกลแบบยืนประจำจุด (Stationary Ranged Enemy)
/// - ยืนอยู่กับที่ ไม่เคลื่อนที่ (ไม่ต้องใช้ NavMesh)
/// - หมุนตัวตามผู้เล่นเมื่ออยู่ในระยะตรวจจับ
/// - ตรวจ Line of Sight (Raycast) ก่อนยิง
/// - แสดงเส้นเล็งเตือน (Warning Laser) ก่อนยิง
/// - ยิง Projectile เข้าหาผู้เล่นตามจังหวะ
/// 
/// วิธีใช้:
/// 1. สร้าง GameObject (ใส่ Sprite + Billboard ถ้าต้องการ)
/// 2. เพิ่ม Component: RangedEnemyAI + EnemyHealth
/// 3. สร้าง Child Empty Object ชื่อ "MuzzlePoint" วางตรงจุดที่กระสุนจะออก
/// 4. ลาก EnemyProjectile Prefab ใส่ช่อง projectilePrefab
/// 5. วางในแมพตรงจุดที่ต้องการ (~1-2 ตัว)
/// </summary>
public class RangedEnemyAI : MonoBehaviour
{
    // ==========================================
    // TARGETING (การตรวจจับ)
    // ==========================================
    [Header("Targeting")]
    [Tooltip("ลาก Player Transform มาใส่ (ถ้าไม่ใส่จะหาจาก Tag 'Player' อัตโนมัติ)")]
    public Transform player;

    [Tooltip("ระยะตรวจจับผู้เล่น (หน่วย Unity unit)")]
    [SerializeField] private float detectionRange = 20f;

    [Tooltip("ระยะยิงได้ (ควรน้อยกว่าหรือเท่ากับ detectionRange)")]
    [SerializeField] private float attackRange = 18f;

    [Tooltip("Layer ที่ถือเป็นสิ่งกีดขวาง (กำแพง/พื้น) สำหรับ Line of Sight")]
    [SerializeField] private LayerMask obstacleLayer;

    // ==========================================
    // COMBAT (การต่อสู้)
    // ==========================================
    [Header("Combat")]
    [Tooltip("Prefab กระสุนที่จะยิงออกมา (ต้องมี EnemyProjectile Script)")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("จุดที่กระสุนจะ Spawn ออกมา (ลาก Child Object มาใส่)")]
    [SerializeField] private Transform muzzlePoint;

    [Tooltip("วินาทีระหว่างกระสุนแต่ละนัด")]
    [SerializeField] private float fireRate = 2f;

    [Tooltip("ความเร็วหมุนตัวตามผู้เล่น")]
    [SerializeField] private float rotationSpeed = 5f;

    [Tooltip("จำนวนกระสุนที่ยิงต่อรอบ (Burst)")]
    [SerializeField] private int burstCount = 1;

    [Tooltip("เวลาระหว่างกระสุนแต่ละนัดใน Burst (วินาที)")]
    [SerializeField] private float burstDelay = 0.15f;

    // ==========================================
    // WARNING LASER (เส้นเล็งเตือน)
    // ==========================================
    [Header("Warning Laser")]
    [Tooltip("เปิด/ปิด เส้นเล็งเตือนก่อนยิง")]
    [SerializeField] private bool enableWarningLaser = true;

    [Tooltip("เวลาแสดงเส้นเล็งก่อนยิง (วินาที)")]
    [SerializeField] private float warningDuration = 0.5f;

    [Tooltip("สีเส้นเล็ง")]
    [SerializeField] private Color laserColor = new Color(1f, 0f, 0f, 0.6f);

    [Tooltip("ความกว้างเส้นเล็ง")]
    [SerializeField] private float laserWidth = 0.03f;

    // ==========================================
    // AUDIO (เสียง)
    // ==========================================
    [Header("Audio")]
    [Tooltip("เสียงยิงกระสุน")]
    [SerializeField] private AudioClip shootSound;

    [Tooltip("เสียง Alert เมื่อตรวจจับผู้เล่น (เล่นครั้งเดียว)")]
    [SerializeField] private AudioClip alertSound;

    [Tooltip("AudioSource สำหรับเล่นเสียง (ถ้าไม่ใส่จะสร้างอัตโนมัติ)")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("ระยะไกลสุดที่ได้ยินเสียง")]
    [SerializeField] private float audioMaxDistance = 25f;

    // ==========================================
    // INTERNAL STATE
    // ==========================================
    private enum EnemyState { Idle, Alert, Attacking, Warning }
    private EnemyState currentState = EnemyState.Idle;

    private float lastFireTime;
    private float warningStartTime;
    private bool hasPlayedAlertSound = false;
    private int currentBurstCount = 0;
    private float lastBurstTime;

    private LineRenderer warningLineRenderer;
    private PlayerHealth playerHealth;

    // ==========================================
    // INITIALIZATION
    // ==========================================
    private void Start()
    {
        // หา Player อัตโนมัติถ้าไม่ได้ assign
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("[RangedEnemyAI] หาผู้เล่นไม่เจอ! ตรวจสอบว่าผู้เล่นมี Tag 'Player'");
            }
        }

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        // สร้าง MuzzlePoint อัตโนมัติถ้าไม่ได้ assign
        if (muzzlePoint == null)
        {
            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(transform);
            muzzle.transform.localPosition = new Vector3(0f, 0.5f, 0.5f); // หน้าตัวศัตรูนิดนึง
            muzzlePoint = muzzle.transform;
        }

        // ตั้งค่า AudioSource สำหรับเสียง 3D
        SetupAudio();

        // ตั้งค่า Warning Laser LineRenderer
        SetupWarningLaser();
    }

    private void SetupAudio()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D Audio 100%
        audioSource.maxDistance = audioMaxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.volume = PlayerPrefs.GetFloat("VFXVol", 1f);
    }

    private void SetupWarningLaser()
    {
        if (!enableWarningLaser) return;

        warningLineRenderer = gameObject.AddComponent<LineRenderer>();
        warningLineRenderer.positionCount = 2;
        warningLineRenderer.startWidth = laserWidth;
        warningLineRenderer.endWidth = laserWidth * 0.5f;
        warningLineRenderer.enabled = false;

        // สร้าง Material เรืองแสงให้เส้นเล็ง
        Material laserMat = new Material(Shader.Find("Sprites/Default"));
        laserMat.color = laserColor;
        warningLineRenderer.material = laserMat;
        warningLineRenderer.startColor = laserColor;
        warningLineRenderer.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.2f);
    }

    // ==========================================
    // MAIN UPDATE LOOP
    // ==========================================
    private void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle(distanceToPlayer);
                break;

            case EnemyState.Alert:
                UpdateAlert(distanceToPlayer);
                break;

            case EnemyState.Warning:
                UpdateWarning(distanceToPlayer);
                break;

            case EnemyState.Attacking:
                UpdateAttacking(distanceToPlayer);
                break;
        }
    }

    // ==========================================
    // STATE: IDLE (ยืนเฉยๆ)
    // ==========================================
    private void UpdateIdle(float distanceToPlayer)
    {
        // ปิด Laser
        if (warningLineRenderer != null) warningLineRenderer.enabled = false;

        // เช็คว่าผู้เล่นเข้ามาในระยะตรวจจับหรือยัง
        if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Alert;
            hasPlayedAlertSound = false;
        }
    }

    // ==========================================
    // STATE: ALERT (ตรวจจับแล้ว หมุนตาม)
    // ==========================================
    private void UpdateAlert(float distanceToPlayer)
    {
        // ผู้เล่นออกนอกระยะ → กลับ Idle
        if (distanceToPlayer > detectionRange)
        {
            currentState = EnemyState.Idle;
            hasPlayedAlertSound = false;
            return;
        }

        // เล่นเสียง Alert ครั้งเดียว
        if (!hasPlayedAlertSound && alertSound != null)
        {
            audioSource.PlayOneShot(alertSound);
            hasPlayedAlertSound = true;
        }

        // หมุนตามผู้เล่น
        RotateTowardsPlayer();

        // เช็คว่าพร้อมยิงหรือยัง
        if (distanceToPlayer <= attackRange && Time.time >= lastFireTime + fireRate)
        {
            // ตรวจ Line of Sight
            if (HasLineOfSight())
            {
                if (enableWarningLaser)
                {
                    // เข้าสู่โหมดเตือนก่อนยิง
                    currentState = EnemyState.Warning;
                    warningStartTime = Time.time;
                }
                else
                {
                    // ยิงเลยไม่ต้องเตือน
                    currentState = EnemyState.Attacking;
                    currentBurstCount = 0;
                }
            }
        }
    }

    // ==========================================
    // STATE: WARNING (แสดงเส้นเล็งเตือน)
    // ==========================================
    private void UpdateWarning(float distanceToPlayer)
    {
        // ผู้เล่นออกนอกระยะ → ยกเลิก
        if (distanceToPlayer > detectionRange)
        {
            currentState = EnemyState.Idle;
            if (warningLineRenderer != null) warningLineRenderer.enabled = false;
            return;
        }

        // หมุนตามผู้เล่นขณะเล็ง
        RotateTowardsPlayer();

        // แสดงเส้นเล็ง (กะพริบ)
        if (warningLineRenderer != null)
        {
            warningLineRenderer.enabled = true;

            Vector3 startPos = muzzlePoint.position;
            Vector3 endPos = player.position + Vector3.up * 0.5f; // เล็งที่หน้าอกผู้เล่น

            warningLineRenderer.SetPosition(0, startPos);
            warningLineRenderer.SetPosition(1, endPos);

            // ทำให้กะพริบ
            float elapsed = Time.time - warningStartTime;
            float blinkSpeed = Mathf.Lerp(4f, 15f, elapsed / warningDuration); // ยิ่งใกล้ยิง ยิ่งกะพริบเร็ว
            bool visible = Mathf.Sin(elapsed * blinkSpeed * Mathf.PI) > 0f;
            warningLineRenderer.enabled = visible;
        }

        // หมดเวลาเตือน → ยิง!
        if (Time.time >= warningStartTime + warningDuration)
        {
            if (warningLineRenderer != null) warningLineRenderer.enabled = false;
            currentState = EnemyState.Attacking;
            currentBurstCount = 0;
        }
    }

    // ==========================================
    // STATE: ATTACKING (ยิงกระสุน)
    // ==========================================
    private void UpdateAttacking(float distanceToPlayer)
    {
        // หมุนตามผู้เล่นขณะยิง
        RotateTowardsPlayer();

        // ยิง Burst
        if (currentBurstCount < burstCount)
        {
            if (currentBurstCount == 0 || Time.time >= lastBurstTime + burstDelay)
            {
                FireProjectile();
                currentBurstCount++;
                lastBurstTime = Time.time;
            }
        }
        else
        {
            // ยิง Burst ครบแล้ว → กลับ Alert รอคูลดาวน์
            lastFireTime = Time.time;
            currentState = EnemyState.Alert;
        }
    }

    // ==========================================
    // CORE FUNCTIONS
    // ==========================================

    /// <summary>
    /// หมุนตัวศัตรูให้หันหน้าไปทางผู้เล่น (เฉพาะแกน Y — ไม่ก้มเงย)
    /// </summary>
    private void RotateTowardsPlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0; // ล็อกแกน Y ไม่ให้เอียง

        if (directionToPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// ตรวจ Line of Sight (Raycast จากตัวเองไปหาผู้เล่น)
    /// คืนค่า true = เห็นผู้เล่น (ไม่มีกำแพงบัง)
    /// </summary>
    private bool HasLineOfSight()
    {
        Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 0.5f;
        Vector3 targetPos = player.position + Vector3.up * 0.5f; // เล็งที่หน้าอก
        Vector3 direction = (targetPos - origin).normalized;
        float distance = Vector3.Distance(origin, targetPos);

        // ยิง Raycast ไปยังผู้เล่น
        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, obstacleLayer))
        {
            // ถ้าชนกำแพง/สิ่งกีดขวางก่อนถึงผู้เล่น → มองไม่เห็น
            return false;
        }

        // ไม่ชนอะไร → เห็นผู้เล่น
        return true;
    }

    /// <summary>
    /// ยิงกระสุนไปยังตำแหน่งผู้เล่น
    /// </summary>
    private void FireProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[RangedEnemyAI] ยังไม่ได้ assign Projectile Prefab!");
            return;
        }

        // คำนวณทิศทางยิงไปยังผู้เล่น (เล็งที่หน้าอก)
        Vector3 spawnPos = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 0.5f;
        Vector3 targetPos = player.position + Vector3.up * 0.5f;
        Vector3 fireDirection = (targetPos - spawnPos).normalized;

        // สร้างกระสุน
        GameObject bullet = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        EnemyProjectile projScript = bullet.GetComponent<EnemyProjectile>();

        if (projScript != null)
        {
            projScript.Initialize(fireDirection);
        }
        else
        {
            Debug.LogWarning("[RangedEnemyAI] Projectile Prefab ไม่มี EnemyProjectile Script!");
            Destroy(bullet, 5f); // ทำลายทิ้งกันค้าง
        }

        // เล่นเสียงยิง
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    // ==========================================
    // EDITOR GIZMOS (แสดงระยะใน Scene View)
    // ==========================================
    private void OnDrawGizmosSelected()
    {
        // วงกลมสีเหลือง = ระยะตรวจจับ
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // วงกลมสีแดง = ระยะยิง
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // เส้นไปยัง Muzzle Point
        if (muzzlePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, muzzlePoint.position);
            Gizmos.DrawSphere(muzzlePoint.position, 0.1f);
        }
    }
}
