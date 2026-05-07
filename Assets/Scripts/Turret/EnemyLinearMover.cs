using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// Enemy를 단순 등속 직선 운동으로 이동시킵니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyLinearMover : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("초당 이동 거리(월드 단위).")]
        private float moveSpeedUnitsPerSecond = 5f;

        [SerializeField]
        [Tooltip("생존 시간(초) 이후 Enemy를 제거합니다.")]
        private float lifeTimeSeconds = 10f;

        private float remainingLifeSeconds;
        private EnemySpawner enemySpawner;

        public void Initialize(float speedUnitsPerSecond, float lifeTime, EnemySpawner spawner)
        {
            moveSpeedUnitsPerSecond = speedUnitsPerSecond;
            lifeTimeSeconds = lifeTime;
            remainingLifeSeconds = lifeTimeSeconds;
            enemySpawner = spawner;
        }

        private void OnEnable()
        {
            remainingLifeSeconds = lifeTimeSeconds;
        }

        private void Update()
        {
            // Projectile과 동일하게 forward 기준 등속 이동으로 단순한 테스트 타겟 행동을 보장합니다.
            transform.position += transform.forward * (moveSpeedUnitsPerSecond * Time.deltaTime);

            remainingLifeSeconds -= Time.deltaTime;
            if (remainingLifeSeconds <= 0f)
            {
                enemySpawner.ReturnToPool(gameObject);
            }
        }
    }
}
