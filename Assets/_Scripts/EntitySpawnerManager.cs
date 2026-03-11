using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitySpawnerManager : NetworkBehaviour
{
    public static EntitySpawnerManager Instance;

    [SerializeField] Transform entityParent;

    [SerializeField] float spawnDelay = 20f;
    [SerializeField] float spawnChance = 0.1f;
    [SerializeField] float spawnCooldown = 10f;
    [SerializeField] int maxEntities = 10;

    readonly List<EntityStats> aliveEntities = new();
    readonly List<EntityStats> deadEntities = new();

    int totalQ => aliveEntities.Count + deadEntities.Count;

    Transform[] spawnerPositions;
    Coroutine enemySpawningCoroutine;
    System.Random rng;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (isServer)
        {
            GameManager.Instance.dngMod.OnDungeonOpens.AddListener(OnDungeonOpens);
            GameManager.Instance.dngMod.OnDungeonCloses.AddListener(OnDungeonCloses);
        }
    }

    private void OnDestroy()
    {
        if (isServer)
        {
            GameManager.Instance.dngMod.OnDungeonOpens.RemoveListener(OnDungeonOpens);
            GameManager.Instance.dngMod.OnDungeonCloses.RemoveListener(OnDungeonCloses);
        }
    }

    private void OnDungeonCloses()
    {
        foreach (var e in aliveEntities)
        {
            if (e == null) continue;
            NetworkServer.Destroy(e.gameObject);
        }

        foreach (var e in deadEntities)
        {
            if (e == null) continue;
            NetworkServer.Destroy(e.gameObject);
        }

        StopCoroutine(enemySpawningCoroutine);
    }

    private void OnDungeonOpens()
    {
        rng = DungeonGenerator.Instance.RNG;
        enemySpawningCoroutine = StartCoroutine(EnemySpawning());
    }

    [Server]
    public void SetSpawnerPositions(List<Transform> positions)
    {
        spawnerPositions = positions.ToArray();
    }

    [Server]
    IEnumerator EnemySpawning()
    {
        float timer = 0f;
        while (timer < spawnDelay)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;
        float cooldownTimer = 0f;
        while (true)
        {
            while (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime;
                yield return null;
            }

            while (aliveEntities.Count >= maxEntities)
            {
                yield return null;
            }

            if (timer >= 5f)
            {
                rng ??= DungeonGenerator.Instance.RNG;
                float rand = (float)(rng.NextDouble() * 100f);

                if (rand <= spawnChance)
                {
                    if (TrySpawnEnemy())
                        cooldownTimer = spawnCooldown;
                }

                timer = 0f;
            }
            
            timer += Time.deltaTime;
            yield return null;
        }
    }

    [Server]
    bool TrySpawnEnemy()
    {
        if (spawnerPositions == null)
        {
            Debug.LogWarning("Spawner positions array is null");
            return false;
        }

        if (spawnerPositions.Length <= 0)
        {
            Debug.LogWarning("Spawner positions array is empty");
            return false;
        }

        ThemeDataSO.EntitySpawn[] entitySpawn = GameManager.Instance.dngMod.ThemeDatas[GameManager.Instance.dngMod.selectedTheme].entitySpawns;

        float totalWeight = 0f;
        foreach (var spawn in entitySpawn)
        {
            totalWeight += spawn.spawnWeight;
        }

        float roll = (float)(rng.NextDouble() * totalWeight);
        float cumulative = 0f;
        foreach (var spawn in entitySpawn)
        {
            cumulative += spawn.spawnWeight;
            int positionIndex = (int)(rng.NextDouble() * spawnerPositions.Length);

            Transform position = spawnerPositions[positionIndex];
            if (position == null) continue;

            if (roll <= cumulative * DungeonGenerator.Instance.GetDificultyMultiplier(position.position))
            {
                GameObject entityObj = Instantiate(spawn.entityPrefab, position.position + position.forward, position.rotation, entityParent);
                entityObj.name = $"{spawn.entityPrefab.name} (ID: {totalQ})";
                NetworkServer.Spawn(entityObj);

                EntityStats stats = entityObj.GetComponentInChildren<EntityStats>();
                aliveEntities.Add(stats);
                return true;
            }
        }

        return false;
    }

    [Server]
    void OnEntityDeath(EntityStats stats)
    {
        if (aliveEntities.Contains(stats))
            aliveEntities.Remove(stats);
        deadEntities.Add(stats);
    }
}
