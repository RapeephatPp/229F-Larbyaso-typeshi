using System.Collections;
using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    [SerializeField] private int ammoAmount = 8;

    [Header("Animation")]
    public float rotateSpeed = 90f;
    public float bobHeight = 0.3f;
    public float bobSpeed = 2f;

    [Header("Collect Effect")]
    public AudioClip collectSound;
    public ParticleSystem collectParticle;

    private Vector3 startPosition;
    private bool isCollected = false;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (isCollected) return;

        // หมุนและลอย
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (other.CompareTag("Player"))
        {  
            var playerShotgun = other.GetComponentInChildren<Shotgun>();
            if (playerShotgun != null)
            {               
                isCollected = true;
                playerShotgun.AddAmmo(ammoAmount);
                StartCoroutine(CollectRoutine());
            }
            else
            {
                Debug.LogWarning("Cannot find shotgun script on Player!");
            }
        }
    }

    private IEnumerator CollectRoutine()
    {
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
}