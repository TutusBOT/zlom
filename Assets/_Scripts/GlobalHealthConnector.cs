 // GlobalHealthConnector.cs (Attach to a persistent GameObject - e.g., Canvas)
 using UnityEngine;
 using System.Collections;

 public class GlobalHealthConnector : MonoBehaviour
 {
  private bool isConnected = false; // Flag to prevent multiple connections

  void Start()
  {
  StartCoroutine(ConnectToHealthController());
  }

  private IEnumerator ConnectToHealthController()
  {
  while (!isConnected)
  {
  // Find the HealthController in the scene.
  HealthController healthController = FindObjectOfType<HealthController>();

  // Find the HealthDisplayer in the scene
  HealthDisplayer healthDisplayer = FindObjectOfType<HealthDisplayer>();

  if (healthController != null && healthDisplayer != null)
  {
  // Connect the HealthController to the HealthDisplayer
  healthController.OnHealthChanged.AddListener(healthDisplayer.UpdateHealthDisplay);
  healthDisplayer.UpdateHealthDisplay(healthController.currentHealth);

  Debug.Log("Successfully connected HealthController to HealthDisplayer.");
  isConnected = true; // Set the flag to stop the Coroutine
  }
  else
  {
  Debug.Log("HealthController or HealthDisplayer not found yet. Waiting...");
  }

  // Wait for a short time before checking again
  yield return new WaitForSeconds(0.5f); // Adjust the wait time as needed
  }
  }
 }
