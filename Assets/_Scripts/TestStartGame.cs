using System.Linq;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;

// THIS SCRIPT IS FOR TESTING PURPOSES ONLY
// IT IS NOT PART OF THE FINAL GAME AND SHOULD NOT BE USED IN PRODUCTION


public class TestStartGame : NetworkBehaviour
{
    [Header("Scene Settings")]
    [SerializeField]
    private string dungeonSceneName = "Dungeon3D";

    [Header("Input Settings")]
    [SerializeField]
    private KeyCode activationKey = KeyCode.Home;

    [SerializeField]
    private bool requireServerAuthority = true;

    private void Update()
    {
        if (Input.GetKeyDown(activationKey))
        {
            Debug.Log($"{activationKey} key pressed - attempting to load dungeon scene");

            bool canTriggerSceneChange = !requireServerAuthority || IsServerInitialized;
            if (canTriggerSceneChange)
            {
                if (IsServerInitialized)
                {
                    LoadDungeonScene();
                }
                else
                {
                    RequestSceneLoadServerRpc();
                }
            }
        }
    }

    private void LoadDungeonScene()
    {
        Debug.Log("Loading dungeon scene for all players");

        // Find all player objects that need to be preserved
        NetworkObject[] playersToMove = FindObjectsOfType<Player>()
            .Select(p => p.GetComponent<NetworkObject>())
            .Where(no => no != null)
            .ToArray();

        Debug.Log($"Moving {playersToMove.Length} players to new scene");

        // Create scene load data
        SceneLoadData sld = new SceneLoadData(dungeonSceneName);
        sld.ReplaceScenes = ReplaceOption.All;

        // Add the players to be moved - this is the key part from the docs
        sld.MovedNetworkObjects = playersToMove;

        // Load the scene and move the players
        NetworkManager.SceneManager.LoadGlobalScenes(sld);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSceneLoadServerRpc()
    {
        LoadDungeonScene();
    }
}
