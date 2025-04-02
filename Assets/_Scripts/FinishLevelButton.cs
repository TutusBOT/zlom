using UnityEngine;

public class FinishLevelButton : MonoBehaviour
{
    public void OnClick()
    {
        SceneController.Instance.LoadScene("Shop");
    }
}
