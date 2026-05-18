using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("ใส่ Player Transform ที่ต้องการให้ AI วิ่งตาม")]
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 5f;
    [Tooltip("ความเร็วในการปีน/กระโดด ข้ามสิ่งกีดขวาง")]
    public float climbSpeed = 3f;

    [Header("Combat Settings")]
    public float attackDamage = 20f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    
    [Header("Audio Settings")]
    [Tooltip("ใส่ AudioSource สำหรับเสียงมอนสเตอร์เวลาวิ่งไล่ล่า")]
    public AudioSource monsterAudioSource;
    [Tooltip("ระยะไกลสุดที่จะได้ยินเสียงมอนสเตอร์")]
    public float audioMaxDistance = 25f;


    private NavMeshAgent agent;
    private PlayerHealth playerHealth;
    private float lastAttackTime;
    private bool isClimbing = false;
    private float lastClimbAttempt = 0f;
    private float lastPathUpdate = 0f;

    private void Start()
    {
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.autoTraverseOffMeshLink = true; 

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            else Debug.LogError("Player not found!");
        }

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }

       
        if (monsterAudioSource != null)
        {
            monsterAudioSource.spatialBlend = 1f; // เป็น 3D 100%
            monsterAudioSource.maxDistance = audioMaxDistance;
            monsterAudioSource.rolloffMode = AudioRolloffMode.Linear;
            
            
            monsterAudioSource.volume = PlayerPrefs.GetFloat("VFXVol", 1f);
            
            
            monsterAudioSource.loop = true;
            if (!monsterAudioSource.isPlaying)
            {
                monsterAudioSource.Play();
            }
        }
    }

    private void Update()
    {
        if (player == null || isClimbing || agent == null) return;

        
        if (!agent.isActiveAndEnabled)
        {
            GhostTrackingUpdate();
            return;
        }

        moveSpeed = agent.speed;

        
        if (agent.acceleration < moveSpeed * 5f) agent.acceleration = moveSpeed * 5f;
        if (agent.angularSpeed < 800f) agent.angularSpeed = 800f;

        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float distanceToPlayerXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(player.position.x, player.position.z));

        if (agent.isOnNavMesh)
        {
            
            if (Time.time > lastPathUpdate + 0.08f)
            {
                
                agent.SetDestination(player.position);
                lastPathUpdate = Time.time;
            }
        }

        
        if (distanceToPlayerXZ <= attackRange && Mathf.Abs(player.position.y - transform.position.y) <= attackRange + 1.5f)
        {
            AttackPlayer();
        }

       
        bool isPathIncomplete = (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid);
        bool isStuckNearPlayer = (agent.pathStatus == NavMeshPathStatus.PathComplete && distanceToPlayerXZ < 4f && (player.position.y - transform.position.y) > 1.5f);

        if (isPathIncomplete || isStuckNearPlayer)
        {
            
            if (agent.velocity.sqrMagnitude < 2.5f)
            {
                TryAutoParkour();
            }
        }
    }

    private void GhostTrackingUpdate()
    {
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float distanceToPlayerXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(player.position.x, player.position.z));

        Vector3 targetDir = (player.position - transform.position).normalized;
        
        
        if (transform.position.y > player.position.y + 0.5f && distanceToPlayerXZ < 2f) 
        {
            targetDir.y -= 2f;
            targetDir.Normalize();
        }
        
        transform.position += targetDir * moveSpeed * Time.deltaTime;
        
        Vector3 lookPlane = new Vector3(player.position.x, transform.position.y, player.position.z);
        if (Vector3.Distance(transform.position, lookPlane) > 0.1f)
            transform.LookAt(lookPlane);

        // โจมตี
        if (distanceToPlayerXZ <= attackRange && Mathf.Abs(player.position.y - transform.position.y) <= attackRange + 1.5f)
        {
            AttackPlayer();
        }

        
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
        {
            if (Mathf.Abs(hit.position.y - transform.position.y) < 0.3f)
            {
                agent.enabled = true;
                if (agent.isActiveAndEnabled)
                    agent.Warp(hit.position);
            }
        }
    }

    private void TryAutoParkour()
    {
        if (Time.time < lastClimbAttempt + 0.3f) return; // ติดคูลดาวน์กันรัวเกิน 0.3 วิ
        lastClimbAttempt = Time.time;

        Vector3 rayStart = transform.position + Vector3.up * 0.5f; 
        
        Vector3 forwardDir = player.position - transform.position;
        forwardDir.y = 0;
        if (forwardDir.sqrMagnitude > 0.01f)
            forwardDir.Normalize();
        else
            forwardDir = transform.forward;

        
        RaycastHit[] fwdHits = Physics.RaycastAll(rayStart, forwardDir, 2.5f);
        bool hitWall = false;
        RaycastHit wallHit = new RaycastHit();
        foreach (var h in fwdHits)
        {
            if (h.collider.transform.root == transform.root || h.collider.transform.root == player.root) continue;
            
            wallHit = h;
            hitWall = true;
            break;
        }

        if (hitWall) 
        {
            Vector3 topDownStart = wallHit.point + (forwardDir * 0.5f) + (Vector3.up * 4f);
            RaycastHit[] downHits = Physics.RaycastAll(topDownStart, Vector3.down, 6f);
            foreach (var r in downHits)
            {
                if (r.collider.transform.root == transform.root || r.collider.transform.root == player.root) continue;
                
                
                if (r.point.y > transform.position.y + 0.6f)
                {
                    StartCoroutine(PerformManualJump(r.point, true));
                    return; 
                }
            }
        }
        else 
        {
            
            Vector3 dropCheckStart = transform.position + (forwardDir * 1.5f) + (Vector3.up * 0.5f);
            RaycastHit[] dropHits = Physics.RaycastAll(dropCheckStart, Vector3.down, 15f);
            foreach (var f in dropHits)
            {
                if (f.collider.transform.root == transform.root || f.collider.transform.root == player.root) continue;
                
                
                if (f.point.y < transform.position.y - 0.8f)
                {
                    StartCoroutine(PerformManualJump(f.point, false));
                    return;
                }
            }
        }
    }

    private IEnumerator PerformManualJump(Vector3 targetPos, bool isJumpingUp)
    {
        isClimbing = true;
        agent.enabled = false; 

        Vector3 finalLandingPos = targetPos;
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
        {
            finalLandingPos = navHit.position;
        }

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        
        
        Vector3 lookDir = finalLandingPos - startPos;
        lookDir.y = 0;
        Quaternion targetRot = startRot;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            targetRot = Quaternion.LookRotation(lookDir);
        }

        float distance = Vector3.Distance(startPos, finalLandingPos);
        
        float jumpTime = Mathf.Max(0.6f, distance / (moveSpeed * 0.8f));
        float journey = 0f;
        
        
        float yDifference = Mathf.Abs(finalLandingPos.y - startPos.y);
        float jumpHeightOffset = isJumpingUp ? (yDifference * 0.4f + 1.2f) : 0.5f;
        
        while (journey < 1f)
        {
            journey += Time.deltaTime / jumpTime;
            
            
            float moveProgress = Mathf.SmoothStep(0f, 1f, journey);
            float heightCurve = Mathf.Sin(Mathf.PI * journey); 
            
            Vector3 lerpPos = Vector3.Lerp(startPos, finalLandingPos, moveProgress);
            
            lerpPos.y += (heightCurve * jumpHeightOffset);
            
            transform.position = lerpPos;
            
            
            float rotProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(journey * 2f));
            transform.rotation = Quaternion.Slerp(startRot, targetRot, rotProgress);
            
            yield return null;
        }

        transform.position = finalLandingPos;
        transform.rotation = targetRot;
        
        
        if (NavMesh.SamplePosition(finalLandingPos, out NavMeshHit validHit, 0.5f, NavMesh.AllAreas))
        {
            if (Mathf.Abs(validHit.position.y - finalLandingPos.y) < 0.5f)
            {
                agent.enabled = true; 
                if (agent.isActiveAndEnabled)
                    agent.Warp(validHit.position);
            }
        }
        
        yield return null;
        isClimbing = false;
    }

    private IEnumerator ClimbOrJump()
    {
        isClimbing = true;
        OffMeshLinkData data = agent.currentOffMeshLinkData;

        
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        
        float journey = 0f;
        while (journey < 1f)
        {
            journey += Time.deltaTime * climbSpeed;
            
            float heightCurve = Mathf.Sin(Mathf.PI * journey); 
            agent.transform.position = Vector3.Lerp(startPos, endPos, journey) + (Vector3.up * heightCurve * 0.3f);
            
            yield return null;
        }

        agent.CompleteOffMeshLink();
        isClimbing = false;
    }

    private void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }

            lastAttackTime = Time.time;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
