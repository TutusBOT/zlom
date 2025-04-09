using System;
using TMPro;
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
    public string breakSoundId = "glass_break";
    public string damageSoundId = "glass_damage";

    public static event Action<GameObject> OnItemBroke;
    protected bool isBeingHeld = false;

    [Header("Value Display")]
    [SerializeField]
    private GameObject valueDisplayPrefab;
    private GameObject activeValueDisplay;
    private TextMeshProUGUI valueText;
    private float tooltipDisplayTime = 1f;
    private float tooltipTimer = 0f;

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

    protected virtual void Start()
    {
        currentCashValue = initialCashValue;
    }

    protected virtual void Update()
    {
        if (isBeingHeld)
        {
            tooltipTimer += Time.deltaTime;

            if (tooltipTimer >= tooltipDisplayTime)
            {
                HideTooltip();
                return;
            }

            activeValueDisplay.transform.position =
                transform.position + Vector3.up * (1.0f * SizeScale);

            if (Camera.main != null)
            {
                activeValueDisplay.transform.forward = Camera.main.transform.forward;
            }

            if (valueText != null)
            {
                valueText.text = $"${Mathf.RoundToInt(currentCashValue)}";
            }
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
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
            return;
        }

        AudioManager.Instance.PlaySound(damageSoundId, transform.position);
    }

    protected virtual void Break()
    {
        Debug.Log($"Item broken! Value lost: {initialCashValue}");
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayEffect("valuable_break", transform.position, SizeScale);
        }
        AudioManager.Instance.PlaySound(breakSoundId, transform.position);

        DestroyValuable();
    }

    public void DestroyValuable()
    {
        OnItemBroke?.Invoke(gameObject);
        HideTooltip();
        Destroy(gameObject);
    }

    public float GetCurrentValue()
    {
        return currentCashValue;
    }

    public virtual void OnPickedUp()
    {
        isBeingHeld = true;
        tooltipTimer = 0f;
        Debug.Log(valueDisplayPrefab);
        Debug.Log(isBeingHeld);

        if (valueDisplayPrefab != null)
        {
            // Create display in world space, NOT as a child
            activeValueDisplay = Instantiate(valueDisplayPrefab);

            // Position it initially
            activeValueDisplay.transform.position =
                transform.position + Vector3.up * (1.5f * SizeScale);

            // Get the text component
            valueText = activeValueDisplay.GetComponentInChildren<TextMeshProUGUI>();

            if (valueText != null)
            {
                valueText.text = $"${Mathf.RoundToInt(currentCashValue)}";
            }
        }
    }

    public virtual void OnDropped()
    {
        isBeingHeld = false;
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (activeValueDisplay != null)
        {
            Destroy(activeValueDisplay);
            activeValueDisplay = null;
            valueText = null;
        }
    }
}
