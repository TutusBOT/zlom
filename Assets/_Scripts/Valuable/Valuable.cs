using System;
using UnityEngine;

public enum ValuableSize
{
    Small,
    Medium,
    Large,
}

public class Valuable : MonoBehaviour
{
    public int initialCashValue = 100;
    private float currentCashValue;

    public float minDamageMultiplier = 0.05f;
    public float maxDamageMultiplier = 0.5f;
    public float breakThreshold = 5f;
    public ValuableSize size;

    public static event Action<GameObject> OnItemBroke;

    private float SizeScale
    {
        get
        {
            return size switch
            {
                ValuableSize.Small => 0.7f,
                ValuableSize.Medium => 1.0f,
                ValuableSize.Large => 1.5f,
                _ => 1.0f,
            };
        }
    }

    private void Start()
    {
        currentCashValue = initialCashValue;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce >= breakThreshold)
        {
            float damagePercent = Mathf.Lerp(
                minDamageMultiplier,
                maxDamageMultiplier,
                impactForce / 20f
            );
            ApplyDamage(damagePercent);
        }
    }

    void ApplyDamage(float damagePercent)
    {
        int damageAmount = Mathf.RoundToInt(initialCashValue * damagePercent);
        currentCashValue -= damageAmount;

        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayEffect("valuable_damage", transform.position, SizeScale);
        }

        Debug.Log($"Item hit! Lost {damageAmount} value. Remaining: {currentCashValue}");

        if (currentCashValue <= 0)
        {
            Break();
        }
    }

    void Break()
    {
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayEffect("valuable_break", transform.position, SizeScale);
        }

        OnItemBroke?.Invoke(gameObject);
        Destroy(gameObject);
    }

    public float GetCurrentValue()
    {
        return currentCashValue;
    }

    public void ShowSellableHighlight(bool show)
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            if (show)
                rend.material.color = Color.Lerp(rend.material.color, Color.green, 0.3f);
            else
                rend.material.color = Color.white;
        }
    }
}
