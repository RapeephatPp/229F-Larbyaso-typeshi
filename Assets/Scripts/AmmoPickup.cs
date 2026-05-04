using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    [SerializeField] private int ammoAmount = 8;

    [Header("Effects")]
    [SerializeField] private AudioClip pickupSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {  
            var playerShotgun = other.GetComponentInChildren<Shotgun>();

            if (playerShotgun != null)
            {               
                playerShotgun.AddAmmo(ammoAmount);
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }
                
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("Cannot found shotgun script");
            }
        }
    }
}
