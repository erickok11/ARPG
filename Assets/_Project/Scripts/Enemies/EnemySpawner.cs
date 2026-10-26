using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Sends waves of monsters at the player. When a wave is cleared the next one comes, a bit bigger,
/// and every few waves the monsters go up a level (more life, damage, experience and better loot).
/// If the player dies, the monsters left are removed and the wave starts over once they respawn.
/// Put this on an empty object in the scene and drag in the Enemy prefab.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;

    [Header("Waves")]
    [SerializeField, Min(1)] private int firstWaveSize = 4;
    [SerializeField, Min(0)] private int extraPerWave = 1;
    [SerializeField, Min(1)] private int maxWaveSize = 15;
    [Tooltip("Seconds of calm before each wave.")]
    [SerializeField] private float timeBetweenWaves = 4f;
    [Tooltip("Seconds between monsters appearing within a wave.")]
    [SerializeField] private float spawnInterval = 0.15f;

    [Header("Monster level")]
    [SerializeField, Min(1)] private int startingMonsterLevel = 1;
    [Tooltip("Monsters go up one level every this many waves.")]
    [SerializeField, Min(1)] private int wavesPerLevel = 2;

    [Header("Placement")]
    [Tooltip("Monsters appear between these distances from the player.")]
    [SerializeField] private float minSpawnDistance = 8f;
    [SerializeField] private float maxSpawnDistance = 14f;
    [Tooltip("Monsters come straight for you instead of waiting until you get close.")]
    [SerializeField] private bool aggroOnSpawn = true;

    private const int SpawnAttempts = 20;

    private readonly List<Health> alive = new List<Health>();
    private Transform player;
    private Health playerHealth;
    private Coroutine waveRoutine;
    private NavMeshPath path;

    /// <summary>The current wave, or the one about to start while NextWaveIn is above 0.</summary>
    public int Wave { get; private set; }
    public int CurrentMonsterLevel => startingMonsterLevel + (Mathf.Max(1, Wave) - 1) / wavesPerLevel;
    public int AliveCount => alive.Count;
    /// <summary>Seconds until the next wave starts (0 once it has started).</summary>
    public float NextWaveIn { get; private set; }

    private void Start()
    {
        path = new NavMeshPath();

        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            player = pc.transform;
            playerHealth = pc.GetComponent<Health>();
            playerHealth.Revived += OnPlayerRevived;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: drag the Enemy prefab into Enemy Prefab.");
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.Revived -= OnPlayerRevived;
    }

    private void Update()
    {
        alive.RemoveAll(h => h == null);

        bool playerDead = playerHealth != null && playerHealth.IsDead;
        if (waveRoutine == null && alive.Count == 0 && !playerDead)
            waveRoutine = StartCoroutine(RunWave(Wave + 1));
    }

    private IEnumerator RunWave(int wave)
    {
        Wave = wave;
        NextWaveIn = timeBetweenWaves;
        while (NextWaveIn > 0f)
        {
            NextWaveIn -= Time.deltaTime;
            yield return null;
        }
        NextWaveIn = 0f;

        int count = Mathf.Min(maxWaveSize, firstWaveSize + extraPerWave * (wave - 1));
        int level = CurrentMonsterLevel;
        Debug.Log($"Wave {wave}: {count} monsters, level {level}.");

        for (int i = 0; i < count; i++)
        {
            Spawn(level);
            yield return new WaitForSeconds(spawnInterval);
        }
        waveRoutine = null;
    }

    private void Spawn(int level)
    {
        if (!TryFindSpawnPoint(out Vector3 position)) return;

        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

        if (!enemy.TryGetComponent(out MonsterLevel monster)) monster = enemy.AddComponent<MonsterLevel>();
        monster.SetLevel(level);

        if (aggroOnSpawn && enemy.TryGetComponent(out EnemyAI ai)) ai.Aggro();

        if (enemy.TryGetComponent(out Health health))
        {
            alive.Add(health);
            health.Died += () => alive.Remove(health);
        }
    }

    /// <summary>A point on the NavMesh, around the player and reachable from where they stand.</summary>
    private bool TryFindSpawnPoint(out Vector3 position)
    {
        Vector3 center = player != null ? player.position : transform.position;

        for (int attempt = 0; attempt < SpawnAttempts; attempt++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            Vector3 candidate = center + new Vector3(direction.x, 0f, direction.y) * Random.Range(minSpawnDistance, maxSpawnDistance);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;

            // Snapping can pull the point close to the player (e.g. near a wall). In a small arena,
            // the last few attempts accept anything rather than spawn nothing.
            Vector3 offset = hit.position - center;
            offset.y = 0f;
            bool lastTries = attempt >= SpawnAttempts - 5;
            if (!lastTries && offset.magnitude < minSpawnDistance * 0.75f) continue;

            if (!NavMesh.CalculatePath(center, hit.position, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete) continue;

            position = hit.position;
            return true;
        }

        position = default;
        return false;
    }

    // The player came back: clear what's left and start this wave again.
    private void OnPlayerRevived()
    {
        if (waveRoutine != null) StopCoroutine(waveRoutine);
        foreach (Health h in alive)
            if (h != null) Destroy(h.gameObject);
        alive.Clear();
        waveRoutine = StartCoroutine(RunWave(Mathf.Max(1, Wave)));
    }
}
