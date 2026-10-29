using System.Collections.Generic;
using UnityEngine;
using SoulsALaCarte.Core;

namespace SoulsALaCarte.Dining
{
    public class DiningCarManager : MonoBehaviour
    {
        public List<CustomerTable> tables = new List<CustomerTable>();
        public Transform customerSpawnPoint;
        public Transform customerExitPoint;

        private float spawnTimer = 0f;
        private float spawnInterval = 15f; // Ritmo pausado y accesible
        private int ticketCounter = 1;
        private int customersSpawnedInRound = 0;

        private bool bossAlreadySpawnedInRound = false;
        private List<GameObject> activeBarriers = new List<GameObject>();

        private void Start()
        {
            if (tables.Count == 0)
            {
                tables.AddRange(FindObjectsByType<CustomerTable>());
            }
        }

        public void StartRound(LevelConfig config)
        {
            ClearAllCustomers();
            ticketCounter = 1;
            customersSpawnedInRound = 0;
            spawnTimer = 2.0f; // El primer cliente llega a los 2 segundos
            bossAlreadySpawnedInRound = false;

            // En nivel 1 (Tutorial) solo spawneará 1 cliente
            if (config.levelIndex == 1)
            {
                spawnInterval = 9999f;
            }
            else
            {
                // Intervalo suave y disfrutable (12s a 16s)
                spawnInterval = Mathf.Max(11.0f, 16.0f - (config.levelIndex * 0.4f));
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.InGameRound)
                return;

            var config = GameManager.Instance.CurrentLevelConfig;
            if (config == null) return;

            // En nivel 1, solo 1 cliente
            if (config.levelIndex == 1 && customersSpawnedInRound >= 1)
            {
                return;
            }

            // Limitar máximo 3 clientes simultáneos para no saturar al jugador
            int activeCount = GetActiveCustomerCount();
            if (activeCount >= 3)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval;
                TrySpawnCustomer();
            }
        }

        public int GetActiveCustomerCount()
        {
            int count = 0;
            var all = FindObjectsByType<CustomerController>();
            foreach (var c in all)
            {
                if (c.state != CustomerState.Leaving) count++;
            }
            return count;
        }

        public void TrySpawnCustomer()
        {
            CustomerTable freeTable = GetFreeTable();
            if (freeTable == null) return;

            var config = GameManager.Instance.CurrentLevelConfig;
            if (config == null) return;

            CustomerClass rolledClass = config.RollClass();
            BossType bType = BossType.None;

            if (!bossAlreadySpawnedInRound && config.ShouldSpawnBoss())
            {
                bType = config.bossType;
                bossAlreadySpawnedInRound = true;
            }

            Vector3 spawnPos = customerSpawnPoint != null ? customerSpawnPoint.position : (transform.position + Vector3.right * 6f);
            Vector3 exitPos = customerExitPoint != null ? customerExitPoint.position : spawnPos;

            GameObject custObj = new GameObject("Customer_" + (bType != BossType.None ? bType.ToString() : rolledClass.ToString()));
            var controller = custObj.AddComponent<CustomerController>();
            controller.Initialize(rolledClass, bType, freeTable, ticketCounter++, spawnPos, exitPos);

            customersSpawnedInRound++;
        }

        public CustomerTable GetFreeTable()
        {
            foreach (var t in tables)
            {
                if (t.IsAvailableForNewCustomer)
                    return t;
            }
            return null;
        }

        public CustomerTable GetAnotherFreeTable(CustomerTable current)
        {
            foreach (var t in tables)
            {
                if (t != current && t.IsAvailableForNewCustomer)
                    return t;
            }
            return null;
        }

        public CustomerTable GetTableWithDirtyPlate()
        {
            foreach (var t in tables)
            {
                if (t.HasDirtyPlate)
                    return t;
            }
            return null;
        }

        public void SpawnTemporaryBarrier(Vector3 position, float duration)
        {
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = "TraitorLabyrinthWall";
            barrier.transform.position = position;
            barrier.transform.localScale = new Vector3(1.2f, 1.8f, 0.4f);
            var rend = barrier.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            rend.material.color = new Color(0.1f, 0.8f, 0.9f);

            activeBarriers.Add(barrier);
            Destroy(barrier, duration);
        }

        public void ClearAllCustomers()
        {
            var all = FindObjectsByType<CustomerController>();
            foreach (var c in all)
            {
                if (c.assignedTable != null)
                {
                    c.assignedTable.HideOrder();
                    c.assignedTable.currentCustomer = null;
                }
                Destroy(c.gameObject);
            }

            foreach (var b in activeBarriers)
            {
                if (b != null) Destroy(b);
            }
            activeBarriers.Clear();
        }
    }
}
