using System.Collections;
using System.Linq;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;

public class SceneController : NetworkBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField]
    private float fadeTime = 0.5f;

    [SerializeField]
    private bool showLoadingScreen = true;

    [Header("References")]
    [SerializeField]
    private CanvasGroup fadeCanvasGroup;

    [SerializeField]
    private GameObject loadingScreen;

    private bool _isLoading = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Initialize fade canvas if needed
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = 0;

            if (loadingScreen != null)
                loadingScreen.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [ObserversRpc(RunLocally = true)]
    public void LoadScene(string sceneName)
    {
        if (!InstanceFinder.IsServerStarted || _isLoading)
            return;

        NetworkObject[] playersToMove = FindObjectsByType<Player>(FindObjectsSortMode.None)
            .Select(p => p.GetComponent<NetworkObject>())
            .Where(no => no != null)
            .ToArray();

        DisablePlayersControlRpc();

        SceneLoadData sld = new SceneLoadData(sceneName);
        sld.ReplaceScenes = ReplaceOption.All;
        sld.MovedNetworkObjects = playersToMove;

        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
        if (IsServerInitialized)
        {
            StartCoroutine(WaitForDungeonGeneration());
        }
    }

    [ObserversRpc(RunLocally = true)]
    public void UnloadScene(string sceneName)
    {
        if (!InstanceFinder.IsServerStarted || _isLoading)
            return;

        SceneUnloadData sud = new SceneUnloadData(sceneName);
        InstanceFinder.SceneManager.UnloadGlobalScenes(sud);
    }

    private IEnumerator WaitForDungeonGeneration()
    {
        // Place players in a safe position (high enough not to fall through)
        ResetAllPlayerPositionsServerRpc();

        // Wait for dungeon to generate
        yield return new WaitForSeconds(2f);

        // Now it's safe to enable player movement
        EnablePlayersControlRpc();

        _isLoading = false;
    }

    [ObserversRpc]
    private void DisablePlayersControlRpc()
    {
        PlayerController[] playerControllers = FindObjectsByType<PlayerController>(
            FindObjectsSortMode.None
        );
        foreach (var controller in playerControllers)
        {
            controller.canMove = false;

            // Disable gravity on character controllers
            CharacterController cc = controller.GetComponent<CharacterController>();
            if (cc != null)
            {
                // Store position so they don't fall
                cc.enabled = false;
            }
        }
    }

    [ObserversRpc]
    private void EnablePlayersControlRpc()
    {
        PlayerController[] playerControllers = FindObjectsByType<PlayerController>(
            FindObjectsSortMode.None
        );
        foreach (var controller in playerControllers)
        {
            // Re-enable character controllers first
            CharacterController cc = controller.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = true;
            }

            // Then allow movement
            controller.canMove = true;
        }
    }

    [ServerRpc(RunLocally = true)]
    private void ResetAllPlayerPositionsServerRpc()
    {
        // Find all players and reset their positions
        Player[] allPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (Player player in allPlayers)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.transform.position = new Vector3(0f, 2f, 0f);
                // Leave controller disabled - will be re-enabled after dungeon generation
            }
        }
    }
}
