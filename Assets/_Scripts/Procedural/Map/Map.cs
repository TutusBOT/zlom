using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Map : NetworkBehaviour
{
    private static Map _instance;
    public static Map Instance => _instance;

    public enum EncounterType
    {
        Shop,
        ForsakenAltar,
        GamblingRoom,
        OfferingRoom,
        ChallengeRoom,
    }

    public enum MainLevelType
    {
        Castle,
        Mansion,
    }

    public enum CurseType
    {
        Labirynth,
        Darkness,
        Distortion,
        Wrath,
        Mirage,
        Fragility,
    }

    [System.Serializable]
    public class Branch
    {
        public bool hasEncounter;
        public EncounterType encounterType;
        public MainLevelType levelType;
        public bool hasCurse;
        public CurseType curseType;

        public override string ToString()
        {
            string result = "";

            if (hasEncounter)
                result += $"Encounter: {encounterType} -> ";

            result += $"Main Level: {levelType}";

            if (hasCurse)
                result += $" (Cursed: {curseType})";

            return result;
        }
    }

    [System.NonSerialized]
    private readonly SyncList<Branch> _currentBranches = new SyncList<Branch>();

    public IReadOnlyList<Branch> CurrentBranches => _currentBranches;

    public int selectedBranch = -1;
    private const float CURSE_CHANCE = 0.5f;
    private const float ENCOUNTER_CHANCE = 0.5f;
    private const int BRANCHES_AMOUNT = 3;

    private readonly SyncDictionary<int, int> _branchVotes = new SyncDictionary<int, int>();

    private readonly SyncVar<bool> _votingActive = new(false);

    private Dictionary<int, int> _playerVotes = new Dictionary<int, int>();

    public delegate void VotingStartedDelegate();
    public event VotingStartedDelegate OnVotingStarted;

    public delegate void VoteUpdatedDelegate(int branchIndex, int voteCount);
    public event VoteUpdatedDelegate OnVoteUpdated;

    public delegate void VotingEndedDelegate(
        int selectedBranch,
        int voteCount,
        float[] probabilities
    );
    public event VotingEndedDelegate OnVotingEnded;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _branchVotes.OnChange += BranchVotesChanged;
    }

    void Start()
    {
        Invoke(nameof(GenerateMap), 1f);
    }

    public void GenerateMap()
    {
        Debug.Log("kadjf;adsfkljdslfsadjfk");
        if (!IsServerInitialized)
            return;

        Debug.Log("Generating new map...");

        for (int i = 0; i < BRANCHES_AMOUNT; i++)
        {
            _currentBranches.Add(GenerateRandomBranch());
        }

        DisplayMapInConsole();

        StartVoting();
    }

    private void BranchVotesChanged(SyncDictionaryOperation op, int key, int value, bool asServer)
    {
        if (op == SyncDictionaryOperation.Set)
        {
            OnVoteUpdated?.Invoke(key, value);
        }
    }

    private Branch GenerateRandomBranch()
    {
        Branch branch = new Branch();

        branch.hasEncounter = Random.value < ENCOUNTER_CHANCE;
        if (branch.hasEncounter)
        {
            branch.encounterType = (EncounterType)
                Random.Range(0, System.Enum.GetValues(typeof(EncounterType)).Length);
        }

        branch.levelType = (MainLevelType)
            Random.Range(0, System.Enum.GetValues(typeof(MainLevelType)).Length);

        branch.hasCurse = Random.value < CURSE_CHANCE;
        if (branch.hasCurse)
        {
            branch.curseType = (CurseType)
                Random.Range(0, System.Enum.GetValues(typeof(CurseType)).Length);
        }

        return branch;
    }

    private void DisplayMapInConsole()
    {
        Debug.Log("=== MAP ===");

        for (int i = 0; i < _currentBranches.Count; i++)
        {
            Debug.Log($"Branch {i + 1}: {_currentBranches[i]}");
        }

        Debug.Log("===========");
    }

    public Branch SelectBranch(int branchIndex)
    {
        if (branchIndex < 0 || branchIndex >= _currentBranches.Count)
        {
            Debug.LogError($"Invalid branch index: {branchIndex}");
            return null;
        }

        selectedBranch = branchIndex;
        Debug.Log($"Selected Branch {branchIndex + 1}: {_currentBranches[branchIndex]}");
        return _currentBranches[branchIndex];
    }

    public Branch GetSelectedBranch()
    {
        if (selectedBranch < 0)
            return null;

        return _currentBranches[selectedBranch];
    }

    public void OnMainLevelCompleted()
    {
        selectedBranch = -1;
        GenerateMap();
    }

    // -------------
    // VOTING SYSTEM

    private void StartVoting()
    {
        // Reset votes
        _branchVotes.Clear();
        selectedBranch = -1;
        for (int i = 0; i < _currentBranches.Count; i++)
        {
            _branchVotes.Add(i, 0);
        }

        _playerVotes.Clear();
        _votingActive.Value = true;

        VotingStartedClientRpc();
    }

    [ObserversRpc]
    private void VotingStartedClientRpc()
    {
        // UI subscribing to this will show voting UI
        OnVotingStarted?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CastVoteServerRpc(int branchIndex, int playerId)
    {
        if (!_votingActive.Value || branchIndex < 0 || branchIndex >= _currentBranches.Count)
            return;

        // If player already voted, remove previous vote
        if (_playerVotes.TryGetValue(playerId, out int previousVote))
        {
            _branchVotes[previousVote] -= 1;
        }

        // Record new vote
        _playerVotes[playerId] = branchIndex;
        _branchVotes[branchIndex] += 1;

        Debug.Log($"Player {playerId} voted for Branch {branchIndex + 1}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void FinalizeVotingServerRpc()
    {
        if (_votingActive.Value)
        {
            FinalizeVoting();
        }
    }

    private void FinalizeVoting()
    {
        _votingActive.Value = false;

        // Calculate total votes
        int totalVotes = 0;
        foreach (var voteCount in _branchVotes.Values)
        {
            totalVotes += voteCount;
        }

        // Calculate probabilities and select branch based on weighted voting
        float[] probabilities = new float[_currentBranches.Count];
        int selectedIndex;

        Debug.Log($"Total votes: {totalVotes}");

        // If no votes, select randomly
        if (totalVotes == 0)
        {
            selectedIndex = Random.Range(0, _currentBranches.Count);

            // All equal probability
            for (int i = 0; i < probabilities.Length; i++)
            {
                probabilities[i] = 1.0f / _currentBranches.Count;
            }
        }
        else
        {
            // Calculate probabilities
            for (int i = 0; i < _currentBranches.Count; i++)
            {
                probabilities[i] = (float)_branchVotes[i] / totalVotes;
            }

            // Weighted random selection
            float randomValue = Random.value;
            float cumulativeProbability = 0f;
            selectedIndex = _currentBranches.Count - 1; // Default to last branch

            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulativeProbability += probabilities[i];
                if (randomValue <= cumulativeProbability)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        // Select the branch
        SelectBranch(selectedIndex);

        // Get vote count for the selected branch
        int selectedVotes = _branchVotes[selectedIndex];

        // Notify clients
        VotingEndedClientRpc(selectedIndex, selectedVotes, probabilities);

        Debug.Log(
            $"Voting ended. Branch {selectedIndex + 1} selected with probability {probabilities[selectedIndex]:P2}"
        );
    }

    [ObserversRpc]
    private void VotingEndedClientRpc(int selectedBranch, int voteCount, float[] probabilities)
    {
        OnVotingEnded?.Invoke(selectedBranch, voteCount, probabilities);
    }

    // Public accessor methods
    public bool IsVotingActive => _votingActive.Value;

    public int GetVoteCount(int branchIndex)
    {
        if (_branchVotes.TryGetValue(branchIndex, out int count))
        {
            return count;
        }
        return 0;
    }

    public int GetTotalVotes()
    {
        int total = 0;
        foreach (var votes in _branchVotes.Values)
        {
            total += votes;
        }
        return total;
    }
}
