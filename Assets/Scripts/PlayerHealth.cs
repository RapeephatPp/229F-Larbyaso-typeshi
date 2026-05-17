using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float CurrentHealth
    { 
        get { return currentHealth; }
        private set { currentHealth = value; }
    }

    [Header("Invincibility Status")]
    private bool isInvincible = false;
    private Coroutine invincibilityRoutine;

    private void Start()
    {
        currentHealth = maxHealth;
        if (PlayerData.HasData == true)
        {
            this.currentHealth = PlayerData.SavedHP;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isInvincible) return;
        currentHealth -= damageAmount;
        Debug.Log("Player took damage: " + damageAmount + ". Current Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ActivateInvincibility(float duration)
    {        
        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
        }
        invincibilityRoutine = StartCoroutine(InvincibilityRoutine(duration));
    }
    private IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        Debug.Log("Player is now INVINCIBLE!");

        // wait for effect if have?

        yield return new WaitForSeconds(duration);

        isInvincible = false;
        Debug.Log("Invincibility ended.");
        invincibilityRoutine = null;
    }
    private void Die()
    {
        Debug.Log("Player has died!");
        PlayerData.ResetData();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}
