using Cysharp.Threading.Tasks;
using Luzart;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
public class EnemySpawnerManager : AbstractMonoBehaviorContent, IRunParticipant
{
    // Runtime dependencies
    private EntityBluePrint _entityBluePrint;
    private PlayerCharacter _player;
    private LevelConfig _currentLevel;
    private Camera _mainCamera;
    private EntityManager _entityManager;
    private GameController _gameController;

    // Spawn settings
    [Header("Spawn Settings")]
    [SerializeField] private float spawnDistanceFromCamera = 2f; // Khoảng cách spawn từ rìa camera
    [SerializeField] private bool spawnOutsideCamera = true; // Spawn bên ngoài camera view
    [SerializeField] private float maxDistanceFromCamera = 15f; // Khoảng cách tối đa trước khi despawn
    [SerializeField] private bool autoDespawnFarEnemies = true; // Tự động despawn enemies xa

    [Header("GameObject Prefab Mode")]
    [Tooltip("Maps EnemyDefinition → DATN GameObject prefab. When set, spawns prefab (Animator/Sprite/Collider2D) " +
             "instead of procedural EnemyCharacter POCO. The prefab's DATNEnemyEntityAdapter auto-registers as IEntity.")]
    [SerializeField] private Luzart.DATNEnemyPrefabRegistry prefabRegistry;
    [Tooltip("Parent transform for instantiated enemy GameObjects (organizes scene hierarchy).")]
    [SerializeField] private Transform spawnParent;

    private List<IEntity> _listEnemy = new List<IEntity>();
    public override void DoInitialize()
    {
        base.DoInitialize();
        _player = _domain.Get<PlayerCharacter>();
        _currentLevel = _domain.Get<LevelConfig>();
        _entityBluePrint = _domain.Get<EntityBluePrint>();
        _mainCamera = _domain.Get<Camera>("MainCamera");
        _entityManager = _domain.Get<EntityManager>();
        _gameController = _domain.Get<GameController>();
    }
    private void DestroyEnemyOld()
    {
        foreach (var item in _listEnemy)
        {
            item.Stop();
            item.Terminate();
        }
        _listEnemy.Clear();
    }

    // ── IRunParticipant ───────────────────────────────────────────────
    void IRunParticipant.OnRunBegin()
    {
        // Wave spawning is kicked by GameController.SpawnNewWave on StartGameplay; nothing to
        // pre-arm here. Reset the tracked list so a fresh run starts clean.
        _listEnemy.Clear();
    }

    /// <summary>Stop spawning and remove every enemy so none keep chasing under the Win/Lose
    /// screen. Spawn loops (SpawnWaveEnemy) run on this component, so StopAllCoroutines halts
    /// them; remaining enemies are GameObjects tagged "Enemy" (Zombie/Monster prefabs).</summary>
    void IRunParticipant.OnRunEnd()
    {
        StopAllCoroutines();
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null) Destroy(enemies[i]);
        }
        _listEnemy.Clear();
    }
    public IEnumerator StartSpawning(List<EnemyWaveConfig> list)
    {
        if (list == null || list.Count == 0) yield break;

        float minDelay = float.MaxValue;
        int countList = list.Count;
        for (int i = 0; i < countList; i++)
        {
            if (list[i].TimeDelayBeforeSpawn < minDelay)
                minDelay = list[i].TimeDelayBeforeSpawn;
        }
        if (minDelay > 0) yield return new WaitForSeconds(minDelay);

        for (int i = 0; i < countList; i++)
        {
            var config = list[i];
            StartCoroutine(SpawnWaveEnemy(config));
        }
    }
    private IEnumerator SpawnWaveEnemy(EnemyWaveConfig config)
    {
        var listStat = config.StatsConfig != null
            ? config.StatsConfig.AssetStats.Select(stat => stat as IStat).ToList()
            : new List<IStat>();
        float spawnInterval = Mathf.Max(0.05f, config.TimeIntervalSpawn);
        var wfs = new WaitForSeconds(spawnInterval);
        float totalTime = 0;
        while (totalTime < config.TotalTimeSpawn)
        {
            if (this == null) yield break;
            totalTime += spawnInterval;
            Vector3 spawnPosition = GetRandomSpawnPositionAroundCamera();
            SpawnOne(spawnPosition, config, listStat);
            yield return wfs;
        }
    }

    /// <summary>
    /// Spawn one enemy at <paramref name="pos"/>. Prefab-first resolution order:
    /// 1) <see cref="EnemyDefinition.Prefab"/> (the primary source — recommended).
    /// 2) <see cref="prefabRegistry"/> fallback (legacy mapping by EnemyDefinition → GameObject).
    /// 3) Skip with warning.
    ///
    /// Visual / scale / sprite / animator / collider are ALL baked into the prefab. The spawner
    /// does NOT override transform.localScale or sprite — those are the prefab's responsibility.
    /// We only pipe per-wave Stats into the on-prefab <see cref="Luzart.LuzartEnemyEntityRoot"/>.
    /// </summary>
    private void SpawnOne(Vector3 pos, EnemyWaveConfig config, List<IStat> statConfig)
    {
        if (config?.EnemyDefinition == null)
        {
            Debug.LogWarning("[EnemySpawnerManager] EnemyWaveConfig.EnemyDefinition is null — skip.");
            return;
        }

        // Primary: prefab on EnemyDefinition itself.
        GameObject prefab = config.EnemyDefinition.Prefab;
        // Fallback: legacy registry lookup.
        if (prefab == null && prefabRegistry != null)
            prefab = prefabRegistry.ResolvePrefab(config.EnemyDefinition);

        if (prefab == null)
        {
            Debug.LogWarning($"[EnemySpawnerManager] {config.EnemyDefinition.name} has no Prefab. " +
                             "Drag a GameObject prefab into EnemyDefinition.prefab field.");
            return;
        }

        var go = Instantiate(prefab, pos, Quaternion.identity);
        if (spawnParent != null) go.transform.SetParent(spawnParent, true);
        // No transform.localScale override — prefab's own scale is the source of truth.

        // F.G.6: LuzartEnemyEntityRoot resolves per-wave HP itself via ResolveWave()
        // in DoStart (reads EnemyData.GetHP(wave)). The legacy DATNEnemyEntityAdapter
        // ApplyStatsConfig path is gone; nothing to do here.
    }
    // -------------------------------------------------------
    private void OnDeadEnemy(IEntity iEntity)
    {
        _gameController.AddEnemyDead(1);
        _listEnemy.Remove(iEntity);
    }
    private Vector3 GetRandomSpawnPositionAroundCamera()
    {
        if (_mainCamera == null)
        {
            return _entityBluePrint.transform.position;
        }
        var cameraBounds = GetCameraBounds();
        int side = Random.Range(0, 4);
        Vector3 spawnPosition = Vector3.zero;
        switch (side)
        {
            case 0: // Top
                spawnPosition = new Vector3(
                    Random.Range(cameraBounds.min.x, cameraBounds.max.x),
                    cameraBounds.max.y + spawnDistanceFromCamera,
                    0
                );
                break;
            case 1: // Right  
                spawnPosition = new Vector3(
                    cameraBounds.max.x + spawnDistanceFromCamera,
                    Random.Range(cameraBounds.min.y, cameraBounds.max.y),
                    0
                );
                break;
            case 2: // Bottom
                spawnPosition = new Vector3(
                    Random.Range(cameraBounds.min.x, cameraBounds.max.x),
                    cameraBounds.min.y - spawnDistanceFromCamera,
                    0
                );
                break;
            case 3: // Left
                spawnPosition = new Vector3(
                    cameraBounds.min.x - spawnDistanceFromCamera,
                    Random.Range(cameraBounds.min.y, cameraBounds.max.y),
                    0
                );
                break;
        }
        return spawnPosition;
    }
    /// <summary>
    /// Lấy bounds của camera trong world space
    /// </summary>
    private Bounds GetCameraBounds()
    {
        if (_mainCamera == null) return new Bounds();
        Vector3 cameraPosition = _mainCamera.transform.position;
        if (_mainCamera.orthographic)
        {
            float height = _mainCamera.orthographicSize * 2f;
            float width = height * _mainCamera.aspect;
            return new Bounds(cameraPosition, new Vector3(width, height, 0));
        }
        else
        {
            float distance = Mathf.Abs(cameraPosition.z);
            float height = 2f * distance * Mathf.Tan(_mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * _mainCamera.aspect;
            return new Bounds(cameraPosition, new Vector3(width, height, 0));
        }
    }
    /// <summary>
    /// Kiểm tra xem enemy có nên bị despawn không (quá xa camera)
    /// </summary>
    private bool ShouldDespawnEnemy(EnemyCharacter enemy)
    {
        if (_mainCamera == null || enemy?.Transform == null) return false;
        Vector3 enemyPosition = enemy.Transform.Position.Value;
        Vector3 cameraPosition = _mainCamera.transform.position;
        float distance = Vector3.Distance(enemyPosition, cameraPosition);
        return distance > maxDistanceFromCamera;
    }
}
//private bool IsPositionInCameraView(Vector3 worldPosition)
//{
//    if (_mainCamera == null) return false;
//    Vector3 viewportPoint = _mainCamera.WorldToViewportPoint(worldPosition);
//    return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
//           viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
//           viewportPoint.z > 0;
//}
///// <summary>
///// Lấy vị trí spawn với offset cụ thể từ camera edge
///// </summary>
//private Vector3 GetSpawnPositionAtDirection(Vector2 direction, float distance = -1)
//{
//    if (_mainCamera == null) return Vector3.zero;
//    if (distance < 0) distance = spawnDistanceFromCamera;
//    var cameraBounds = GetCameraBounds();
//    Vector3 cameraCenter = cameraBounds.center;
//    // Normalize direction
//    direction = direction.normalized;
//    // Calculate position at camera edge + offset
//    Vector3 edgePosition = cameraCenter + new Vector3(
//        direction.x * (cameraBounds.size.x * 0.5f),
//        direction.y * (cameraBounds.size.y * 0.5f),
//        0
//    );
//    // Add extra distance outside camera
//    Vector3 spawnPosition = edgePosition + new Vector3(direction.x * distance, direction.y * distance, 0);
//    return spawnPosition;
//}
///// <summary>
///// Spawn enemies trong pattern hình tròn xung quanh camera
///// </summary>
//public void SpawnCircularPattern(EnemyDefinition enemyDef, int count, float radius = -1, List<Stat> stats = null)
//{
//    if (!isStarted || _mainCamera == null) return;
//    if (radius < 0)
//        radius = spawnDistanceFromCamera;
//    Vector3 cameraCenter = _mainCamera.transform.position;
//    Material mat = new Material(enemyDef.Material);
//    for (int i = 0; i < count; i++)
//    {
//        float angle = (360f / count) * i * Mathf.Deg2Rad;
//        Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
//        Vector3 spawnPosition = cameraCenter + offset;
//        var enemy = SpawnEnemy(spawnPosition, enemyDef, stats ?? new List<Stat>(), mat);
//        if (enemy != null)
//        {
//            listEnemyCharacter.Add(enemy);
//        }
//    }
//}