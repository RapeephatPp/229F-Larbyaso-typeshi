using UnityEngine;

public class InvincibilityOrb : MonoBehaviour
{
    [Header("Orb Settings")]
    [SerializeField] private float invincibilityDuration = 7f;

    [Header("Effects")]
    [SerializeField] private AudioClip pickupSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var playerHealth = other.GetComponent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.ActivateInvincibility(invincibilityDuration);

                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                Destroy(gameObject);
            }
        }
    }
}
