using UnityEngine;

public class Map : MonoBehaviour
{
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

    private Branch[] _currentBranches = new Branch[3];

    private int _selectedBranch = -1;
    private const float CURSE_CHANCE = 0.5f;
    private const float ENCOUNTER_CHANCE = 0.5f;
    private const int BRANCHES_AMOUNT = 3;

    void Start()
    {
        GenerateMap();

        Invoke(nameof(GenerateMap), 2f);
    }

    public void GenerateMap()
    {
        Debug.Log("Generating new map...");

        for (int i = 0; i < BRANCHES_AMOUNT; i++)
        {
            _currentBranches[i] = GenerateRandomBranch();
        }

        DisplayMapInConsole();
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

        for (int i = 0; i < _currentBranches.Length; i++)
        {
            Debug.Log($"Branch {i + 1}: {_currentBranches[i]}");
        }

        Debug.Log("===========");
    }

    public Branch SelectBranch(int branchIndex)
    {
        if (branchIndex < 0 || branchIndex >= _currentBranches.Length)
        {
            Debug.LogError($"Invalid branch index: {branchIndex}");
            return null;
        }

        _selectedBranch = branchIndex;
        Debug.Log($"Selected Branch {branchIndex + 1}: {_currentBranches[branchIndex]}");
        return _currentBranches[branchIndex];
    }

    public Branch GetSelectedBranch()
    {
        if (_selectedBranch < 0)
            return null;

        return _currentBranches[_selectedBranch];
    }

    public void OnMainLevelCompleted()
    {
        _selectedBranch = -1;
        GenerateMap();
    }
}
