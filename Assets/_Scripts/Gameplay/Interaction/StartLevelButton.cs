using UnityEngine;

public class StartLevelButton : MonoBehaviour
{
    public void OnClick()
    {
        BootstrapNetworkManager.ChangeNetworkScene("Dungeon3D", new string[] { "Train" });
    }
}
