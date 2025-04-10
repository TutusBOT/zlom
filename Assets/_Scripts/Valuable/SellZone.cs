using System.Collections.Generic;
using UnityEngine;

public class SellZone : MonoBehaviour
{
    [Header("Settings")]
    public float interactionRange = 2f;
    public KeyCode sellKey = KeyCode.E;

    [Tooltip("Bonus percentage (1.0 = normal price, 1.5 = 50% bonus)")]
    public float valueMultiplier = 1.2f;

    [Header("Visual")]
    public Color zoneColor = new Color(0, 1, 0, 0.2f);
    public GameObject sellPromptUI; // Reference to a UI element that shows "Press E to sell"

    public bool debug;

    private List<Valuable> valuablesInZone = new List<Valuable>();
    private bool playerInRange = false;
    private Transform playerTransform;

    private void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = zoneColor;
        }

        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Hide the prompt initially
        if (sellPromptUI != null)
            sellPromptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        Valuable valuable = other.GetComponent<Valuable>();
        if (valuable != null && !valuablesInZone.Contains(valuable))
        {
            valuablesInZone.Add(valuable);
        }

        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerTransform = other.transform;

            EnableKeyDetection(true);

            // Show prompt if we have items to sell
            if (sellPromptUI != null && valuablesInZone.Count > 0)
                sellPromptUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Valuable valuable = other.GetComponent<Valuable>();
        if (valuable != null)
        {
            valuablesInZone.Remove(valuable);
        }

        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            EnableKeyDetection(false);

            if (sellPromptUI != null)
                sellPromptUI.SetActive(false);
        }
    }

    private void EnableKeyDetection(bool enable)
    {
        if (enable)
        {
            PlayerInputHandler.OnSellKeyPressed += HandleSellKeyPressed;
        }
        else
        {
            PlayerInputHandler.OnSellKeyPressed -= HandleSellKeyPressed;
        }
    }

    private void HandleSellKeyPressed()
    {
        if (playerInRange && valuablesInZone.Count > 0)
        {
            if (debug)
                Debug.Log("Sell key pressed!");
            SellAllValuables();

            if (sellPromptUI != null)
                sellPromptUI.SetActive(false);
        }
    }

    private void SellAllValuables()
    {
        int totalValue = 0;

        foreach (Valuable valuable in new List<Valuable>(valuablesInZone))
        {
            if (valuable != null)
            {
                float value = valuable.GetCurrentValue() * valueMultiplier;
                totalValue += Mathf.RoundToInt(value);

                valuable.DestroyValuable();
            }
        }

        valuablesInZone.Clear();

        if (totalValue > 0)
        {
            PlayerMoneyManager.Instance.AddMoney(totalValue);
        }
    }

    private void OnDrawGizmos()
    {
        if (!debug)
            return;
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
