using Unity.Entities;

namespace SubwayFreedomAssetPack.Domain
{
    public class StationUIUpgradeData
    {
        public StationUIElement[] Upgrades;
    }

    public class StationUIElement
    {
        public Entity Entity;
        public string Name;
    }
}
