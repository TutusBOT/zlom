public interface IUpgradeable
{
    bool CanHandleUpgrade(UpgradeType type);
    void ApplyUpgrade(UpgradeType type, int level, float value);
}
