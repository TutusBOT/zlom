using System.Collections.Generic;
using UnityEngine;

public class SellZone : MonoBehaviour
{
    [Header("Settings")]
    public float valueMultiplier = 1.2f;

    [Header("Visual")]
    public Color zoneColor = new Color(0, 1, 0, 0.2f);

    [Tooltip("Optional debug mode")]
    public bool debug;

    private HashSet<Valuable> previousValuables = new HashSet<Valuable>();

    private void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = zoneColor;
        }
    }

    private void Update()
    {
        Collider[] colliders = Physics.OverlapBox(transform.position, transform.localScale / 2);
        HashSet<Valuable> currentValuables = new HashSet<Valuable>();

        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Valuable"))
            {
                Valuable valuable = collider.GetComponent<Valuable>();
                if (valuable != null)
                {
                    currentValuables.Add(valuable);
                    if (!previousValuables.Contains(valuable))
                    {
                        valuable.SetInvulnerable(true);
                        if (debug) Debug.Log($"{valuable.name} is now INVULNERABLE");
                    }
                }
            }
        }

        // Any valuables that were in last frame but not now, disable their invulnerability
        foreach (Valuable oldValuable in previousValuables)
        {
            if (!currentValuables.Contains(oldValuable))
            {
                oldValuable.SetInvulnerable(false);
                if (debug) Debug.Log($"{oldValuable.name} is now VULNERABLE");
            }
        }

        previousValuables = currentValuables;
    }

    public void TrySellItems()
    {
        List<Valuable> valuablesToSell = new List<Valuable>();

        Collider[] colliders = Physics.OverlapBox(transform.position, transform.localScale / 2);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Valuable"))
            {
                Valuable valuable = collider.GetComponent<Valuable>();
                if (valuable != null)
                {
                    valuablesToSell.Add(valuable);
                }
            }
        }

        if (valuablesToSell.Count == 0)
        {
            if (debug) Debug.Log("No valuables in sell zone.");
            return;
        }

        SellValuables(valuablesToSell);
    }

    private void SellValuables(List<Valuable> valuables)
    {
        int totalValue = 0;

        foreach (Valuable valuable in valuables)
        {
            if (valuable != null)
            {
                float value = valuable.GetCurrentValue() * valueMultiplier;
                totalValue += Mathf.RoundToInt(value);

                // Remove from invulnerable list
                valuable.SetInvulnerable(false);
                previousValuables.Remove(valuable);

                valuable.DestroyValuable();
            }
        }

        if (totalValue > 0)
        {
            EconomyManager.Instance.AddMoneyServerRpc(totalValue);
            if (debug) Debug.Log($"Sold valuables for {totalValue} currency.");
        }
    }

    private void OnDrawGizmos()
    {
        if (!debug) return;

        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
