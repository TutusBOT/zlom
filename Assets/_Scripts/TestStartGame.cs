using UnityEngine;

// THIS SCRIPT IS FOR TESTING PURPOSES ONLY
// IT IS NOT PART OF THE FINAL GAME AND SHOULD NOT BE USED IN PRODUCTION

public class TestStartGame : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField]
    private string dungeonSceneName = "Dungeon3D";

    [SerializeField]
    private KeyCode activationKey = KeyCode.Home;

    private void Update()
    {
        if (Input.GetKeyDown(activationKey))
        {
            Debug.Log($"{activationKey} key pressed - attempting to load dungeon scene");
            SceneController.Instance.LoadScene("Dungeon3D");
        }
    }
}
