using UnityEngine;

public class BootspeedItem : MonoBehaviour
{
    [Header("Speed Boost Settings")]    
    [SerializeField] private float speedMultiplier = 1.6f;   
    [SerializeField] private float boostDuration = 7f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var pl = other.GetComponent<PlayerMovement>();
            if (pl != null)
            {              
                pl.ApplySpeedBoost(speedMultiplier, boostDuration);
                Destroy(gameObject);
            }
        }
    }
    
}
