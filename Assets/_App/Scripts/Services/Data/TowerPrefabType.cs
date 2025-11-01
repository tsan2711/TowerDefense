namespace Services.Data
{
    /// <summary>
    /// Enum để mapping các Tower prefab trong Resources/Tower
    /// Sử dụng MainTower enum từ Tower.cs để map từ backend config sang Unity Tower prefab
    /// </summary>
    public enum TowerPrefabType
    {
        Emp = 0,
        Laser = 1,
        MachineGun = 2,
        Pylon = 3,
        Rocket = 4,
        SuperTower = 5,
    }
}
