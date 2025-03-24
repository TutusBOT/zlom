 using UnityEngine;
 using TMPro;

 public class HealthDisplayer : MonoBehaviour
 {
  public TextMeshProUGUI healthText;

  public void Start()
  {
    UpdateHealthDisplay(100f);
  }

  public void UpdateHealthDisplay(float health)
  {
  if (healthText != null)
  {
  healthText.text = "Health: " + health.ToString("F0");
  }
  }
 }