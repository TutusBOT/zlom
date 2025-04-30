using UnityEngine;

public class StartLevelButton : MonoBehaviour
{
    public void OnClick()
    {
        SceneController.Instance.LoadScene("Dungeon3D");
    }
}
