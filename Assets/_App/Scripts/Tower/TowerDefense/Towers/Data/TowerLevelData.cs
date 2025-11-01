using UnityEngine;

namespace TowerDefense.Towers.Data
{
	/// <summary>
	/// Data container for settings per tower level
	/// </summary>
	[CreateAssetMenu(fileName = "TowerData.asset", menuName = "TowerDefense/Tower Configuration", order = 1)]
	public class TowerLevelData : ScriptableObject
	{
		public TowerType towerType;	
		/// <summary>
		/// A description of the tower for displaying on the UI
		/// </summary>
		public string description;

		/// <summary>
		/// A description of the tower for displaying on the UI
		/// </summary>
		public string upgradeDescription;

		/// <summary>
		/// The cost to upgrade to this level
		/// </summary>
		public int cost;

		/// <summary>
		/// The sell cost of the tower
		/// </summary>
		public int sell;

		/// <summary>
		/// The max health
		/// </summary>
		public int maxHealth;

		/// <summary>
		/// The starting health
		/// </summary>
		public int startingHealth;

		/// <summary>
		/// The tower icon
		/// </summary>
		public Sprite icon;
	}

	public enum TowerType
	{
		Emp1 = 0,
		Emp2 = 1,
		Emp3 = 2,
		Laser1 = 3,
		Laser2 = 4,
		Laser3 = 5,
		MachineGun1 = 6,
		MachineGun2 = 7,
		MachineGun3 = 8,
		Pylon1 = 9,
		Pylon2 = 10,
		Pylon3 = 11,
		Rocket1 = 12,
		Rocket2 = 13,
		Rocket3 = 14,
		SuperTower = 15,
	}
}

