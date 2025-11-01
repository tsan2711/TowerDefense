using System;
using UnityEngine;

namespace Services.Data
{
    /// <summary>
    /// Serializable data model for AgentConfiguration from Firebase Firestore
    /// Used to sync AgentConfiguration data from backend
    /// </summary>
    [Serializable]
    public class AgentConfigurationData
    {
        /// <summary>
        /// Agent type as integer (matches backend enum)
        /// </summary>
        public int type;

        /// <summary>
        /// Theác name of the agent
        /// </summary>
        public string agentName;

        /// <summary>
        /// Short summary of the agent
        /// </summary>
        public string agentDescription;

        /// <summary>
        /// Convert to AgentConfiguration ScriptableObject data
        /// </summary>
        public TowerDefense.Agents.Data.AgentType GetAgentType()
        {
            return (TowerDefense.Agents.Data.AgentType)type;
        }
    }
}

