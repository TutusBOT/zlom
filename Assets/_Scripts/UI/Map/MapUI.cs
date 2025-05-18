using System.Collections;
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

public class MapUI : NetworkBehaviour
{
    [SerializeField]
    private GameObject mapCanvasRoot;

    [SerializeField]
    private RectTransform[] branchUIElements = new RectTransform[3];

    [SerializeField]
    private Button[] branchButtons = new Button[3];

    [SerializeField]
    private TMPro.TextMeshProUGUI[] branchDescriptions = new TMPro.TextMeshProUGUI[3];

    [SerializeField]
    private Image[] branchIcons = new Image[3];

    [SerializeField]
    private TMPro.TextMeshProUGUI[] voteCountTexts = new TMPro.TextMeshProUGUI[3];

    // Visual elements for each type
    [SerializeField]
    private Sprite[] encounterIcons;

    [SerializeField]
    private Sprite[] levelIcons;

    [SerializeField]
    private Sprite[] curseIcons;

    private bool _isShown = false;
    private Map _map;
    private bool _hasVoted = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        _map = Map.Instance;

        if (_map != null)
        {
            _map.OnVotingStarted += HandleVotingStarted;
            _map.OnVoteUpdated += HandleVoteUpdated;
            _map.OnVotingEnded += HandleVotingEnded;
        }
    }

    private void OnDisable()
    {
        if (_map != null)
        {
            _map.OnVotingStarted -= HandleVotingStarted;
            _map.OnVoteUpdated -= HandleVoteUpdated;
            _map.OnVotingEnded -= HandleVotingEnded;
        }
    }

    private void Start()
    {
        _isShown = false;
        mapCanvasRoot.SetActive(false);

        // Setup branch button listeners for voting
        for (int i = 0; i < branchButtons.Length; i++)
        {
            int index = i; // Capture for lambda
            branchButtons[i].onClick.AddListener(() => VoteForBranch(index));
        }

        // Initialize vote count texts
        for (int i = 0; i < voteCountTexts.Length; i++)
        {
            voteCountTexts[i].text = "0";
        }
    }

    private void Update()
    {
        if (InputBindingManager.Instance.IsActionTriggered(InputActions.ToggleMap))
        {
            Debug.Log(_map);
            ToggleMap();
        }
    }

    private void VoteForBranch(int branchIndex)
    {
        int playerId = -1;

        // Try to find the local player
        if (PlayerManager.Instance != null)
        {
            var localPlayer = PlayerManager.Instance.GetLocalPlayer();
            if (localPlayer != null && localPlayer.NetworkObject != null)
            {
                playerId = localPlayer.NetworkObject.ObjectId;
            }
        }

        if (playerId != -1 && !_hasVoted)
        {
            _map.CastVoteServerRpc(branchIndex, playerId);
            _hasVoted = true;

            // Visually indicate that we've voted
            SetVotedButtonState(branchIndex);

            // If server, check if all players have voted
            if (IsServerInitialized)
            {
                CheckAllPlayersVoted();
            }
        }
    }

    private void SetVotedButtonState(int votedIndex)
    {
        // Highlight the voted option and disable all options
        for (int i = 0; i < branchButtons.Length; i++)
        {
            if (i == votedIndex)
            {
                // Visual highlight for selected option
                branchButtons[i].GetComponent<Image>().color = new Color(0.7f, 0.9f, 0.7f);
            }
            else
            {
                // Dim other options
                branchButtons[i].GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
            }

            // Disable interaction with all buttons after voting
            branchButtons[i].interactable = false;
        }
    }

    private void CheckAllPlayersVoted()
    {
        if (!IsServerInitialized)
            return;

        // Get total player count and compare to votes count
        int playerCount = 0;
        if (PlayerManager.Instance != null)
        {
            playerCount = PlayerManager.Instance.GetAllPlayers().Count;
        }

        if (playerCount == 0)
            return;

        // Check if all players have voted
        if (_map.GetTotalVotes() >= playerCount)
        {
            // All players have voted, finalize
            _map.FinalizeVotingServerRpc();
        }
    }

    private void HandleVotingStarted()
    {
        _hasVoted = false;

        for (int i = 0; i < branchButtons.Length; i++)
        {
            branchButtons[i].interactable = true;
            branchButtons[i].GetComponent<Image>().color = Color.white;
            voteCountTexts[i].text = "0";
        }

        // Update branch descriptions
        for (int i = 0; i < branchDescriptions.Length; i++)
        {
            branchButtons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>().text =
                _map.CurrentBranches[i].ToString();
        }

        // Update branch icons based on branch types
        for (int i = 0; i < branchIcons.Length; i++)
        {
            Map.Branch branch = _map.CurrentBranches[i];
            UpdateBranchIcon(i, branch);
        }
    }

    private void UpdateBranchIcon(int index, Map.Branch branch)
    {
        return; // Currently disabled

        if (branchIcons[index] != null)
        {
            Sprite iconToUse = null;

            if (branch.hasEncounter)
            {
                int encounterIndex = (int)branch.encounterType;
                if (encounterIcons != null && encounterIndex < encounterIcons.Length)
                {
                    iconToUse = encounterIcons[encounterIndex];
                }
            }
            else
            {
                int levelIndex = (int)branch.levelType;
                if (levelIcons != null && levelIndex < levelIcons.Length)
                {
                    iconToUse = levelIcons[levelIndex];
                }
            }

            if (iconToUse != null)
            {
                branchIcons[index].sprite = iconToUse;
                branchIcons[index].gameObject.SetActive(true);
            }
            else
            {
                branchIcons[index].gameObject.SetActive(false);
            }

            // Apply curse overlay if needed
            if (branch.hasCurse)
            {
                branchUIElements[index].GetComponent<Image>().color = new Color(0.8f, 0.3f, 0.3f);
            }
            else
            {
                branchUIElements[index].GetComponent<Image>().color = Color.white;
            }
        }
    }

    private void HandleVoteUpdated(int branchIndex, int voteCount)
    {
        if (branchIndex >= 0 && branchIndex < voteCountTexts.Length)
        {
            voteCountTexts[branchIndex].text = voteCount.ToString();
        }

        if (IsServerInitialized)
        {
            CheckAllPlayersVoted();
        }
    }

    private void HandleVotingEnded(int selectedBranch, int voteCount, float[] probabilities)
    {
        // Highlight winning branch
        for (int i = 0; i < branchUIElements.Length; i++)
        {
            voteCountTexts[i].text = i == selectedBranch ? "Winner!" : "Loser";

            // Disable all buttons
            branchButtons[i].interactable = false;
        }

        // Hide map UI after delay
        StartCoroutine(HideMapAfterDelay(3f));
    }

    private IEnumerator HideMapAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ToggleMap(false);
    }

    public void ToggleMap(bool? show = null)
    {
        bool showMap = show ?? !_isShown;

        Debug.Log($"Toggling map: {showMap}");
        Debug.Log($"Map UI: {mapCanvasRoot}");
        if (showMap)
        {
            _isShown = true;
            mapCanvasRoot.SetActive(true);
            Canvas canvas = mapCanvasRoot.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                // Force canvas refresh
                canvas.enabled = false;
                canvas.enabled = true;
            }
            PlayerManager.Instance.GetLocalPlayer().ToggleControls(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            mapCanvasRoot.SetActive(false);
            _isShown = false;
            PlayerManager.Instance.GetLocalPlayer().ToggleControls(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
