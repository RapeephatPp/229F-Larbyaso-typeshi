using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Shotgun : MonoBehaviour
{
    [Header("Gun Stats")]
    [SerializeField] private int pelletCount = 12; 
    [SerializeField] private float damagePerPellet = 10f; 
    [SerializeField] private float spreadAngle = 7f; 
    [SerializeField] private float range = 30f; 
    
    [Header("Ammo & Fire Rate")]
    [SerializeField] private float fireRate = 1f; 
    [SerializeField] private int magSize = 5; 
    [SerializeField] private int startingTotalAmmo = 15; 
    [SerializeField] private int maxTotalAmmo = 50; 
    [SerializeField] private KeyCode reloadKey = KeyCode.R; 

    // ==========================================
    // ระบบชาร์จยิง (Charge Shot & Visuals)
    // ==========================================
    [Header("Charge Mechanics")]
    [SerializeField] private float chargeTimeRequired = 1f; 
    [SerializeField] private float chargedDamageMultiplier = 2f; 
    [SerializeField] private float chargedRecoilMultiplier = 1.5f; 
    [SerializeField] private float chargeShakeAmount = 5f; 
    
    [Header("Charge Visuals (UI & Colors)")]
    [SerializeField] private Image chargeGaugeUI; // ลาก UI หลอดชาร์จมาใส่ตรงนี้
    [SerializeField] private Color normalTraceColor = Color.yellow; // สีปกติ
    [SerializeField] private Color chargedTraceColor = Color.cyan; // สีฟ้าตอนชาร์จเต็ม

    private bool isCharging = false;
    private float currentChargeTime = 0f;
    // ==========================================
    
    [Header("Effects")]
    [SerializeField] private GameObject bulletTracePrefab;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float gunVolumeScale = 1f;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip emptyGunSound;
    private AudioSource audioSource;

    [Header("Mobility & External Recoil")]
    [SerializeField] private float playerRecoilForce = 15f; 

    [Header("2D Animation")]
    [SerializeField] private RectTransform gunRectTransform; 
    [SerializeField] private Image gunImage; 
    [SerializeField] private Sprite idleSprite; 
    [SerializeField] private Sprite[] fireFrames; 
    [SerializeField] private Sprite[] pumpFrames; 
    [SerializeField] private float timePerFrame = 0.05f; 
    
    [Header("Dynamic Settings")]
    [SerializeField] private float swayAmount = 8f;
    [SerializeField] private float maxSwayAmount = 15f;
    [SerializeField] private float swaySmoothness = 10f;
    [SerializeField] private float bobSpeed = 12f;
    [SerializeField] private float bobAmount = 15f;
    [SerializeField] private Vector2 recoilKickback = new Vector2(0f, -150f); 
    [SerializeField] private float recoilRotation = 12f; 
    [SerializeField] private float recoilRecoverySpeed = 10f; 

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;

    private bool isShooting = false;
    private bool isReloading = false;
    private Vector2 originalPosition;
    private float nextFireTime = 0f;
    
    private int currentAmmo; 
    private int totalAmmo;   

    private Vector2 currentSway;
    private Vector2 currentBob;
    private float bobTimer;
    private Vector2 currentRecoilPos;
    private float currentRecoilRot;

    void Start()
    {
        currentAmmo = magSize; 
        totalAmmo = startingTotalAmmo;

        if (gunRectTransform != null) originalPosition = gunRectTransform.anchoredPosition;
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (gunImage != null && idleSprite != null) gunImage.sprite = idleSprite;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = PlayerPrefs.GetFloat("VFXVol", 1f) * gunVolumeScale;

        // ซ่อนหลอดชาร์จตอนเริ่มเกม
        if (chargeGaugeUI != null) chargeGaugeUI.fillAmount = 0f;
    }

    void Update()
    {   
        if (Time.timeScale == 0f) return;
        
        if (Input.GetMouseButtonDown(0) && !isShooting && !isReloading && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                isCharging = true;
                currentChargeTime = 0f;
            }
            else if (totalAmmo > 0)
            {
                StartCoroutine(ReloadSequence());
            }
            else
            {
                if (emptyGunSound != null) 
                {
                    audioSource.clip = emptyGunSound;
                    audioSource.Play();
                }
            }
        }

        // --- ระบบอัปเดตหลอดชาร์จ ---
        if (isCharging)
        {
            if (Input.GetMouseButton(0))
            {
                currentChargeTime += Time.deltaTime; 
                // ค่อยๆ เติมหลอด UI ให้เต็มตามเปอร์เซ็นต์
                if (chargeGaugeUI != null)
                {
                    chargeGaugeUI.fillAmount = Mathf.Clamp01(currentChargeTime / chargeTimeRequired);
                    
                    // เปลี่ยนสีหลอดให้รู้ว่าเต็มแล้ว
                    if (currentChargeTime >= chargeTimeRequired)
                        chargeGaugeUI.color = chargedTraceColor;
                    else
                        chargeGaugeUI.color = normalTraceColor;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                bool isChargedShot = currentChargeTime >= chargeTimeRequired;
                StartCoroutine(ShootSequence(isChargedShot));
                
                isCharging = false;
                currentChargeTime = 0f;
                if (chargeGaugeUI != null) chargeGaugeUI.fillAmount = 0f; // ล้างหลอดชาร์จ
            }
        }

        if (Input.GetKeyDown(reloadKey) && !isShooting && !isReloading && currentAmmo < magSize && totalAmmo > 0)
        {
            isCharging = false; 
            if (chargeGaugeUI != null) chargeGaugeUI.fillAmount = 0f;
            StartCoroutine(ReloadSequence());
        }

        HandleDynamicWeaponFeel();
    }

    private void HandleDynamicWeaponFeel()
    {
        if (gunRectTransform == null) return;

        float mouseX = -Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = -Input.GetAxis("Mouse Y") * swayAmount;
        mouseX = Mathf.Clamp(mouseX, -maxSwayAmount, maxSwayAmount);
        mouseY = Mathf.Clamp(mouseY, -maxSwayAmount, maxSwayAmount);
        currentSway = Vector2.Lerp(currentSway, new Vector2(mouseX, mouseY), Time.deltaTime * swaySmoothness);

        float currentSpeed = playerMovement != null ? playerMovement.GetCurrentSpeed() : 0f;
        if (currentSpeed > 1f) 
        {
            bobTimer += Time.deltaTime * bobSpeed * (currentSpeed / 10f); 
            currentBob.x = Mathf.Cos(bobTimer / 2f) * bobAmount; 
            currentBob.y = Mathf.Sin(bobTimer) * bobAmount;      
        }
        else 
        {
            bobTimer = 0f;
            currentBob = Vector2.Lerp(currentBob, Vector2.zero, Time.deltaTime * 5f);
        }

        currentRecoilPos = Vector2.Lerp(currentRecoilPos, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
        currentRecoilRot = Mathf.Lerp(currentRecoilRot, 0f, Time.deltaTime * recoilRecoverySpeed);

        Vector2 chargeShake = Vector2.zero;
        if (isCharging && currentChargeTime >= chargeTimeRequired)
        {
            chargeShake = new Vector2(Random.Range(-chargeShakeAmount, chargeShakeAmount), Random.Range(-chargeShakeAmount, chargeShakeAmount));
        }

        gunRectTransform.anchoredPosition = originalPosition + currentSway + currentBob + currentRecoilPos + chargeShake;
        float tiltOffset = currentSway.x * 0.2f; 
        gunRectTransform.localRotation = Quaternion.Euler(0, 0, currentRecoilRot + tiltOffset);
    }

    private IEnumerator ShootSequence(bool isChargedShot)
    {
        isShooting = true;
        currentAmmo--; 
        nextFireTime = Time.time + fireRate; 

        if (shootSound != null) 
        {
            audioSource.clip = shootSound;
            audioSource.Play();
        } 

        float recoilForce = isChargedShot ? playerRecoilForce * chargedRecoilMultiplier : playerRecoilForce;
        float damageMult = isChargedShot ? chargedDamageMultiplier : 1f;
        float visualKickMult = isChargedShot ? 1.5f : 1f; 

        float randomRot = Random.Range(-recoilRotation, recoilRotation);
        float randomX = Random.Range(-30f, 30f); 
        
        currentRecoilPos = recoilKickback * visualKickMult + new Vector2(randomX, 0); 
        currentRecoilRot = randomRot * visualKickMult; 

        if (fireFrames.Length > 0)
        {
            gunImage.sprite = fireFrames[0];
            // ส่งสถานะชาร์จไปที่ลูกปราย
            FirePellets(damageMult, isChargedShot); 

            if (playerMovement != null)
            {
                Vector3 recoilDir = -playerCamera.transform.forward;
                playerMovement.ApplyRecoil(recoilDir * recoilForce);
            }

            yield return new WaitForSeconds(timePerFrame);

            for (int i = 1; i < fireFrames.Length; i++)
            {
                gunImage.sprite = fireFrames[i];
                yield return new WaitForSeconds(timePerFrame);
            }
        }

        if (pumpFrames.Length > 0)
        {
            for (int i = 0; i < pumpFrames.Length; i++)
            {
                gunImage.sprite = pumpFrames[i];
                yield return new WaitForSeconds(timePerFrame);
            }
        }

        gunImage.sprite = idleSprite;
        isShooting = false;
    }

    private IEnumerator ReloadSequence()
    {
        isReloading = true;
        if (reloadSound != null) 
        {
            audioSource.clip = reloadSound;
            audioSource.Play();
        }

        int ammoNeeded = magSize - currentAmmo;
        if (totalAmmo < ammoNeeded) ammoNeeded = totalAmmo; 

        currentRecoilPos = new Vector2(0, -50f);
        currentRecoilRot = 15f;

        for (int round = 0; round < 2; round++)
        {
            if (pumpFrames.Length > 0)
            {
                for (int i = 0; i < pumpFrames.Length; i++)
                {
                    gunImage.sprite = pumpFrames[i];
                    yield return new WaitForSeconds(timePerFrame * 1.5f);
                }
            }
        }

        totalAmmo -= ammoNeeded;
        currentAmmo += ammoNeeded; 
        gunImage.sprite = idleSprite;
        isReloading = false;
    }

    private void FirePellets(float damageMultiplier, bool isChargedShot)
    {
        Vector3 fakeBarrelEnd = playerCamera.transform.position + playerCamera.transform.forward * 0.8f - playerCamera.transform.up * 0.2f + playerCamera.transform.right * 0.2f;
        float finalDamage = damagePerPellet * damageMultiplier;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 spread = playerCamera.transform.forward;
            spread = Quaternion.AngleAxis(Random.Range(-spreadAngle, spreadAngle), playerCamera.transform.up) * spread;
            spread = Quaternion.AngleAxis(Random.Range(-spreadAngle, spreadAngle), playerCamera.transform.right) * spread;

            if (Physics.Raycast(playerCamera.transform.position, spread, out RaycastHit hit, range))
            {
                EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
                if (enemy != null) enemy.TakeDamage(finalDamage);
                
                SpawnTrace(fakeBarrelEnd, hit.point, isChargedShot);
            }
            else
            {
                SpawnTrace(fakeBarrelEnd, playerCamera.transform.position + spread * range, isChargedShot);
            }
        }
    }

    private void SpawnTrace(Vector3 start, Vector3 end, bool isChargedShot)
    {
        if (bulletTracePrefab != null)
        {
            GameObject trace = Instantiate(bulletTracePrefab, start, Quaternion.identity);
            BulletTrace traceScript = trace.GetComponent<BulletTrace>();
            
            if (traceScript != null)
            {
                traceScript.SetTrace(start, end);
                
                // สั่งเปลี่ยนสีตามสถานะการชาร์จ
                if (isChargedShot)
                {
                    // เปลี่ยนเป็นสีฟ้า (ตั้งค่าไว้ใน Inspector)
                    traceScript.SetColor(chargedTraceColor, Color.blue);
                }
                else
                {
                    // ใช้สีปกติ
                    traceScript.SetColor(normalTraceColor, new Color(1f, 0.5f, 0f)); // สีส้มปลายๆ
                }
            }
        }
    }

    public void AddAmmo(int amount)
    {
        totalAmmo += amount;
        if (totalAmmo > maxTotalAmmo) totalAmmo = maxTotalAmmo; 
    }

    public void ApplySettingsFromSave()
    {
        if (audioSource != null)
        {
            audioSource.volume = PlayerPrefs.GetFloat("VFXVol", 1f) * gunVolumeScale;
        }
    }

    public int GetCurrentAmmo() { return currentAmmo; }
    public int GetTotalAmmo() { return totalAmmo; }
}