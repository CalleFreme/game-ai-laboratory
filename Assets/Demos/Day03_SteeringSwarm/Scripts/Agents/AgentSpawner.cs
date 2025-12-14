using Demos.Day03_SteeringSwarm.Scripts.AI;
using UnityEngine;

namespace Demos.Day03_SteeringSwarm.Scripts.Agents
{
    public class AgentSpawner : MonoBehaviour
    {
        public SteeringAgent agentPrefab;
        public int agentCount = 10;
        public Vector2 spawnAreaSize = new Vector2(10f, 10f);

        void Start()
        {
            for (int i = 0; i < agentCount; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
                    0f,
                    Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f)
                );

                Vector3 spawnPos = transform.position + offset;
                Instantiate(agentPrefab, spawnPos, Quaternion.identity);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));
        }

        void Update()
        {

        }
    }
}
