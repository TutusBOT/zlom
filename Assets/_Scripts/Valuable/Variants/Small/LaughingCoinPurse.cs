using UnityEngine;

public class LaughingCoinPurse : Valuable
{
    [Header("Coin Purse Properties")]
    [SerializeField]
    private float minLaughInterval = 15f;

    [SerializeField]
    private float maxLaughInterval = 45f;

    [SerializeField]
    private float laughDuration = 5f;

    [SerializeField]
    private float shakeThreshold = 2.5f; // Shake sensitivity

    [SerializeField]
    private string laughSoundId = "purse_laugh";

    [SerializeField]
    private string silencedSoundId = "purse_silence";

    [Header("Visual Effects")]
    [SerializeField]
    private GameObject faceObject; // Reference to the face part

    [SerializeField]
    private Material normalMaterial;

    [SerializeField]
    private Material laughingMaterial; // More expressive face

    // Internal state
    private float timeUntilNextLaugh;
    private bool isLaughing = false;
    private float laughTimer = 0f;
    private Vector3 lastPosition;
    private float movementAccumulator = 0f;
    private Renderer faceRenderer;
    private bool hasBeenDiscovered = false;

    protected override void Start()
    {
        base.Start();

        // Set as small valuable
        size = ValuableSize.Small;

        // Set up face renderer if assigned
        if (faceObject != null)
        {
            faceRenderer = faceObject.GetComponent<Renderer>();
        }

        // Initialize first laugh timer
        timeUntilNextLaugh = Random.Range(minLaughInterval, maxLaughInterval);
        lastPosition = transform.position;
    }

    protected override void Update()
    {
        base.Update();

        if (!hasBeenDiscovered)
            return;

        if (!isLaughing)
        {
            // Count down to next laugh
            timeUntilNextLaugh -= Time.deltaTime;

            if (timeUntilNextLaugh <= 0f)
            {
                StartLaughing();
            }
        }
        else
        {
            // Already laughing
            laughTimer += Time.deltaTime;

            // Check if being held and shaken
            if (isBeingHeld)
            {
                CheckForShaking();
            }

            // If laughed too long without being silenced
            if (laughTimer >= laughDuration)
            {
                ApplyStressEffect();
                StopLaughing();
            }
        }
    }

    private void StartLaughing()
    {
        isLaughing = true;
        laughTimer = 0f;

        // Play laugh sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(laughSoundId, transform.position);
        }

        // Change face to laughing expression
        if (faceRenderer != null && laughingMaterial != null)
        {
            faceRenderer.material = laughingMaterial;
        }

        Debug.Log("The coin purse starts laughing mischievously!");
    }

    private void StopLaughing()
    {
        isLaughing = false;

        // Reset laugh timer
        timeUntilNextLaugh = Random.Range(minLaughInterval, maxLaughInterval);

        // Reset face to normal expression
        if (faceRenderer != null && normalMaterial != null)
        {
            faceRenderer.material = normalMaterial;
        }
    }

    private void CheckForShaking()
    {
        // Calculate movement since last frame
        Vector3 currentPos = transform.position;
        float movement = Vector3.Distance(currentPos, lastPosition);

        // Debug to see actual movement values
        Debug.Log($"Movement: {movement}, Accumulator: {movementAccumulator}");

        // Accumulate movement with less decay and higher weight
        movementAccumulator += movement * 5f; // Amplify small movements
        movementAccumulator *= 0.8f; // Slower decay (was 0.9f)

        // Use lastPosition even if not silenced
        lastPosition = currentPos;

        // Lower threshold for easier triggering
        if (movementAccumulator > shakeThreshold)
        {
            Debug.Log($"Silenced at accumulator value: {movementAccumulator}");
            SilencePurse();
            movementAccumulator = 0f;
        }
    }

    private void SilencePurse()
    {
        // Play silenced sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(silencedSoundId, transform.position);
        }

        Debug.Log("The coin purse stops laughing after being shaken!");
        StopLaughing();
    }

    private void ApplyStressEffect()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("Player stress increases from the annoying laughing purse!");
        }
    }

    public override void OnPickedUp()
    {
        base.OnPickedUp();

        if (!hasBeenDiscovered)
        {
            hasBeenDiscovered = true;
            Debug.Log("The coin purse has been discovered!");
        }

        lastPosition = transform.position;
        movementAccumulator = 0f;
    }
}
