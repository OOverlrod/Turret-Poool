using UnityEngine;
using UnityEngine.Pool;

namespace TurretDemo
{
    /// <summary>
    /// 랜덤 SpawnPoint에서 Enemy를 생성하고 최대 개수를 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GameObject enemyPrefab;

        [SerializeField]
        [Tooltip("랜덤 생성 위치 목록.")]
        private Transform[] spawnPoints;

        [SerializeField]
        [Tooltip("생성된 Enemy의 부모(선택).")]
        private Transform enemyRoot;

        [Header("Spawn Settings")]
        [SerializeField]
        [Min(1)]
        private int initialSpawnCount = 4;

        [SerializeField]
        [Min(1)]
        private int maxAliveCount = 8;

        [SerializeField]
        [Min(0.1f)]
        private float spawnIntervalSeconds = 1f;

        [SerializeField]
        [Tooltip("생성 시 Turret 중앙점을 향하도록 forward를 배치합니다.")]
        private Transform lookAtCenter;

        [SerializeField]
        [Min(0.1f)]
        private float enemyMoveSpeedUnitsPerSecond = 5f;

        [SerializeField]
        [Min(0.1f)]
        private float enemyLifeTimeSeconds = 10f;

        //private readonly List<GameObject> aliveEnemies = new List<GameObject>(32);

        private ObjectPool<GameObject> pool;
        private float nextSpawnTimeSeconds;

        private void Awake()
        {
            pool = new ObjectPool<GameObject>(
                createFunc: CreateItem,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyItem,
                collectionCheck: true,
                defaultCapacity: initialSpawnCount,
                maxSize: maxAliveCount
                );

        }
        private void Start()
        {
            for (int spawnIndex = 0; spawnIndex < initialSpawnCount; spawnIndex++)
            {
                if (!TrySpawnOneEnemy())
                {
                    break;
                }
            }
        }

        private void Update()
        {
            if (Time.time < nextSpawnTimeSeconds)
            {
                return;
            }

            if (TrySpawnOneEnemy())
            {
                nextSpawnTimeSeconds = Time.time + spawnIntervalSeconds;
            }
        }

        private bool TrySpawnOneEnemy()
        {
            if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                return false;
            }

            if (pool.CountActive >= maxAliveCount)
            {
                return false;
            }

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (spawnPoint == null)
            {
                return false;
            }

            Quaternion rotation = spawnPoint.rotation;
            if (lookAtCenter != null)
            {
                Vector3 toCenter = lookAtCenter.position - spawnPoint.position;
                if (toCenter.sqrMagnitude > 1e-8f)
                {
                    rotation = Quaternion.LookRotation(toCenter.normalized, Vector3.up);
                }
            }

            GameObject spawned = pool.Get();
            spawned.transform.SetPositionAndRotation(spawnPoint.position, rotation);

            EnemyLinearMover mover = spawned.GetComponent<EnemyLinearMover>();
            if (mover != null)
            {
                mover.Initialize(enemyMoveSpeedUnitsPerSecond, enemyLifeTimeSeconds, this);
            }

            EnemyTarget enemyTarget = spawned.GetComponent<EnemyTarget>();
            if (enemyTarget != null)
            {
                enemyTarget.Initialize(this);
            }

            return true;
        }

        public void ReturnToPool(GameObject enemy)
        {
            if (enemy == null)
            {
                return;
            }
            pool.Release(enemy);
        }

        private GameObject CreateItem()
        {
            //GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //생성해두고
            GameObject gameObject = Instantiate(enemyPrefab, enemyRoot); //비활성화하고
            gameObject.SetActive(false);
            //enque pool 넣기  (x)
            return gameObject;
        }

        // Called when an item is taken from the pool.
        private void OnGet(GameObject gameObject)
        {
            //대여하기 -꺼내쓰기 
            // SetActive - true 로 켜주기
            // deque 안해도 됨 
            gameObject.SetActive(true);
        }

        // Called when an item is returned to the pool.
        private void OnRelease(GameObject gameObject)
        {
            // 반납하기
            // 삭제 X -->  꺼주기 
            // enqueue XXX - 내부적으로 됨 
            gameObject.SetActive(false);
        }

        // Called when the pool decides to destroy an item (e.g., above max size).
        private void OnDestroyItem(GameObject gameObject)
        {
            //maxsize를 넘을시, 삭제 할때
            Destroy(gameObject);
        }
    }
}
