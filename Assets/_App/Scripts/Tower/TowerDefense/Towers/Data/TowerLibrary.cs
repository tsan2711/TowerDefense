using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerDefense.Towers.Data
{
	/// <summary>
	/// The asset which holds the list of different towers
	/// </summary>
	[CreateAssetMenu(fileName = "TowerLibrary.asset", menuName = "TowerDefense/Tower Library", order = 1)]
	public class TowerLibrary : ScriptableObject, IList<Tower>, IDictionary<string, Tower>
	{
		/// <summary>
		/// The list of all the towers
		/// </summary>
		public List<Tower> configurations;

		/// <summary>
		/// The internal reference to the dictionary made from the list of towers
		/// with the name of tower as the key
		/// </summary>
		Dictionary<string, Tower> m_ConfigurationDictionary;

		/// <summary>
		/// The accessor to the towers by index
		/// </summary>
		/// <param name="index"></param>
		public Tower this[int index]
		{
			get { return configurations[index]; }
		}

		public void OnBeforeSerialize()
		{
		}

		/// <summary>
		/// Convert the list (m_Configurations) to a dictionary for access via name
		/// </summary>
		public void OnAfterDeserialize()
		{
			if (configurations == null)
			{
				return;
			}
			m_ConfigurationDictionary = configurations.ToDictionary(t => t.towerName);
		}

		/// <summary>
		/// Ensure dictionary is initialized before use
		/// </summary>
		private void EnsureDictionaryInitialized()
		{
			if (m_ConfigurationDictionary == null)
			{
				if (configurations == null)
				{
					configurations = new List<Tower>();
					m_ConfigurationDictionary = new Dictionary<string, Tower>();
					return;
				}
				
				// Build dictionary manually to handle null towers and duplicate names
				m_ConfigurationDictionary = new Dictionary<string, Tower>();
				foreach (var tower in configurations)
				{
					if (tower != null && !string.IsNullOrEmpty(tower.towerName))
					{
						// Skip if duplicate key (shouldn't happen but safe)
						if (!m_ConfigurationDictionary.ContainsKey(tower.towerName))
						{
							m_ConfigurationDictionary[tower.towerName] = tower;
						}
					}
				}
			}
		}

		public bool ContainsKey(string key)
		{
			EnsureDictionaryInitialized();
			return m_ConfigurationDictionary.ContainsKey(key);
		}

		public void Add(string key, Tower value)
		{
			EnsureDictionaryInitialized();
			m_ConfigurationDictionary.Add(key, value);
		}

		public bool Remove(string key)
		{
			EnsureDictionaryInitialized();
			return m_ConfigurationDictionary.Remove(key);
		}

		public bool TryGetValue(string key, out Tower value)
		{
			EnsureDictionaryInitialized();
			return m_ConfigurationDictionary.TryGetValue(key, out value);
		}

		Tower IDictionary<string, Tower>.this[string key]
		{
			get 
			{ 
				EnsureDictionaryInitialized();
				return m_ConfigurationDictionary[key]; 
			}
			set 
			{ 
				EnsureDictionaryInitialized();
				m_ConfigurationDictionary[key] = value; 
			}
		}

		public ICollection<string> Keys
		{
			get 
			{ 
				EnsureDictionaryInitialized();
				return ((IDictionary<string, Tower>) m_ConfigurationDictionary).Keys; 
			}
		}

		ICollection<Tower> IDictionary<string, Tower>.Values
		{
			get 
			{ 
				EnsureDictionaryInitialized();
				return m_ConfigurationDictionary.Values; 
			}
		}

		IEnumerator<KeyValuePair<string, Tower>> IEnumerable<KeyValuePair<string, Tower>>.GetEnumerator()
		{
			EnsureDictionaryInitialized();
			return m_ConfigurationDictionary.GetEnumerator();
		}

		public void Add(KeyValuePair<string, Tower> item)
		{
			EnsureDictionaryInitialized();
			m_ConfigurationDictionary.Add(item.Key, item.Value);
		}

		public bool Remove(KeyValuePair<string, Tower> item)
		{
			EnsureDictionaryInitialized();
			return m_ConfigurationDictionary.Remove(item.Key);
		}

		public bool Contains(KeyValuePair<string, Tower> item)
		{
			EnsureDictionaryInitialized();
			return m_ConfigurationDictionary.Contains(item);
		}

		public void CopyTo(KeyValuePair<string, Tower>[] array, int arrayIndex)
		{
			int count = array.Length;
			for (int i = arrayIndex; i < count; i++)
			{
				Tower config = configurations[i - arrayIndex];
				KeyValuePair<string, Tower> current = new KeyValuePair<string, Tower>(config.towerName, config);
				array[i] = current;
			}
		}

		public int IndexOf(Tower item)
		{
			return configurations.IndexOf(item);
		}

		public void Insert(int index, Tower item)
		{
			configurations.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			configurations.RemoveAt(index);
		}

		Tower IList<Tower>.this[int index]
		{
			get { return configurations[index]; }
			set { configurations[index] = value; }
		}

		public IEnumerator<Tower> GetEnumerator()
		{
			return configurations.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable) configurations).GetEnumerator();
		}

		public void Add(Tower item)
		{
			configurations.Add(item);
		}

		public void Clear()
		{
			configurations.Clear();
		}

		public bool Contains(Tower item)
		{
			return configurations.Contains(item);
		}

		public void CopyTo(Tower[] array, int arrayIndex)
		{
			configurations.CopyTo(array, arrayIndex);
		}

		public bool Remove(Tower item)
		{
			return configurations.Remove(item);
		}

		public int Count
		{
			get { return configurations.Count; }
		}

		public bool IsReadOnly
		{
			get { return ((ICollection<Tower>) configurations).IsReadOnly; }
		}
	}
}