using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class EntitySpawnerManager : NetworkBehaviour
{
    public static EntitySpawnerManager Instance;

    [SerializeField] GameObject[] entityPrefabs;

    [SerializeField] float spawnDelay = 20f;
    [SerializeField] float spawnChance = 0.1f;
    [SerializeField] float spawnCooldown = 10f;
    [SerializeField] int maxEntities = 10;

    List<EntityStats> aliveEntities = new();
    List<EntityStats> deadEntities = new();

    Transform[] spawnerPositions;
    float spawnTimer;
    bool spawning = false;

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
            GameTick.OnSecond += OnSecond;
    }

    private void OnDestroy()
    {
        if (isServer)
            GameTick.OnSecond -= OnSecond;
    }

    [Server]
    public void SetSpawnerPositions(List<Transform> positions)
    {
        spawnerPositions = positions.ToArray();
    }

    [Server]
    void OnSecond()
    {
        if (!GameManager.Instance.gameStarted) return;

        if (aliveEntities.Count >= maxEntities) return;

        spawnTimer += 1f;

        if (!spawning & spawnTimer < spawnDelay) return;
        spawning = true;

        if (spawnTimer < spawnCooldown) return;

        spawnTimer = 0f;
        float rand = Random.Range(0f, 100f);
        if (rand > spawnChance) return;

        int entityIndex = Random.Range(0, entityPrefabs.Length);
        int positionIndex = Random.Range(0, spawnerPositions.Length);
        Transform position = spawnerPositions[positionIndex];
        GameObject entityObj = Instantiate(entityPrefabs[entityIndex], position.position + position.forward, position.rotation);
        NetworkServer.Spawn(entityObj);

        EntityStats stats = entityObj.GetComponentInChildren<EntityStats>();
        aliveEntities.Add(stats);
    }

    [Server]
    void OnEntityDeath(EntityStats stats)
    {
        if (aliveEntities.Contains(stats))
            aliveEntities.Remove(stats);
        deadEntities.Add(stats);
    }
}
