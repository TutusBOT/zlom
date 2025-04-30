using System.Collections.Generic;
using UnityEngine;

public class PlayerUpgrades : MonoBehaviour
{
    private Dictionary<UpgradeType, int> upgradeCount = new Dictionary<UpgradeType, int>();

    [System.Serializable]
    public class UpgradeDefinition
    {
        public UpgradeType type;
        public string name;
        public string description;
        public Sprite icon;

        [Tooltip("Maximum level this upgrade can reach (1 for one-time upgrades)")]
        public int maxLevel = 3;

        [Tooltip("If true, can be stacked beyond maxLevel")]
        public bool canStackInfinitely = false;

        [Tooltip("Base value for the first upgrade level")]
        public float baseValue = 0.2f;

        [Tooltip("How much value increases per level")]
        public float valuePerLevel = 0.1f;

        public float GetValueForLevel(int level)
        {
            if (level <= 0)
                return 0f;
            return baseValue + (valuePerLevel * (level - 1));
        }

        public bool CanUpgradeFurther(int currentLevel)
        {
            if (canStackInfinitely)
                return true;
            return currentLevel < maxLevel;
        }
    }

    [SerializeField]
    private List<UpgradeDefinition> availableUpgrades = new List<UpgradeDefinition>();
    private Dictionary<UpgradeType, UpgradeDefinition> upgradeLookup =
        new Dictionary<UpgradeType, UpgradeDefinition>();

    private void Awake()
    {
        foreach (var upgrade in availableUpgrades)
        {
            upgradeLookup[upgrade.type] = upgrade;
        }
    }

    public bool HasUpgrade(UpgradeType type)
    {
        return upgradeCount.ContainsKey(type) && upgradeCount[type] > 0;
    }

    public int GetUpgradeLevel(UpgradeType type)
    {
        if (upgradeCount.ContainsKey(type))
            return upgradeCount[type];

        return 0;
    }

    public float GetUpgradeValue(UpgradeType type)
    {
        int level = GetUpgradeLevel(type);
        if (level == 0)
            return 0f;

        if (upgradeLookup.TryGetValue(type, out var definition))
            return definition.GetValueForLevel(level);

        return 0f;
    }

    public UpgradeDefinition GetUpgradeDefinition(UpgradeType type)
    {
        if (upgradeLookup.TryGetValue(type, out var definition))
            return definition;

        return null;
    }

    public bool CanUpgradeFurther(UpgradeType type)
    {
        var definition = GetUpgradeDefinition(type);
        if (definition == null)
            return false;

        int currentLevel = GetUpgradeLevel(type);
        return definition.CanUpgradeFurther(currentLevel);
    }

    public void ApplyUpgrade(UpgradeType type)
    {
        var definition = GetUpgradeDefinition(type);
        if (definition == null)
            return;

        int currentLevel = GetUpgradeLevel(type);
        if (!definition.CanUpgradeFurther(currentLevel))
            return;

        int newLevel = currentLevel + 1;
        upgradeCount[type] = newLevel;

        ApplyUpgradeToComponents(type, newLevel, definition.GetValueForLevel(newLevel));
    }

    private void ApplyUpgradeToComponents(UpgradeType type, int level, float value)
    {
        // Find all upgradeable components on Player
        IUpgradeable[] upgradeables = GetComponentsInChildren<IUpgradeable>();

        bool handled = false;
        foreach (var upgradeable in upgradeables)
        {
            if (upgradeable.CanHandleUpgrade(type))
            {
                upgradeable.ApplyUpgrade(type, level, value);
                handled = true;
            }
        }

        if (!handled)
        {
            Debug.LogWarning($"No component found to handle upgrade: {type}");
        }
    }
}

public enum UpgradeType
{
    Speed,
    Stamina,
    Health,
    Strength,
    Range,
}
