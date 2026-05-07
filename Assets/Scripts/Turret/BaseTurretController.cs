using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// 포탑류 공통 제어 흐름(조준→판정→발사)을 담당하는 추상 부모 클래스입니다. (Template Method + OCP)
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class BaseTurretController : MonoBehaviour, ITurretAimDebugState
    {
        [Header("References")]
        [SerializeField]
        private Transform yawPivot;

        [SerializeField]
        private Transform pitchPivot;

        [SerializeField]
        private Transform muzzlePoint;

        [SerializeField]
        private GameObject projectilePrefab;

        [SerializeField]
        [Tooltip("생성된 발사체의 부모(선택). 비워두면 월드 루트에 생성됩니다.")]
        private Transform projectileSpawnRoot;

        [Header("Yaw")]
        [SerializeField]
        [Tooltip("Yaw 회전 속도(도/초). 높이 성분은 제거한 방향만 사용.")]
        private float yawSpeedDegreesPerSecond = 90f;

        [Header("Pitch")]
        [SerializeField]
        [Tooltip("Pitch 회전 속도(도/초). PitchPivot.localRotation(X)만 사용.")]
        private float pitchSpeedDegreesPerSecond = 60f;

        [SerializeField]
        private float minPitchDegrees = -45f;

        [SerializeField]
        private float maxPitchDegrees = 20f;

        [Header("Fire Control")]
        [SerializeField]
        [Tooltip("MuzzlePoint.forward와 타겟 방향 사이 허용 각(도) 이하일 때 조준 완료.")]
        private float fireAngleThresholdDegrees = 5f;

        [SerializeField]
        private float fireIntervalSeconds = 0.5f;

        [SerializeField]
        private float projectileSpeed = 12f;

        [SerializeField]
        private float projectileLifeTimeSeconds = 3f;

        [SerializeField]
        [Tooltip("Projectile 1발당 데미지량.")]
        private float projectileDamage = 10f;

        [SerializeField]
        [Tooltip("이 Turret의 팀 식별자입니다.")]
        private int turretTeamId;

        [SerializeField]
        [Tooltip("0 이하면 무제한. 초과하면 Muzzle 기준 거리 밖 타겟은 발사하지 않습니다.")]
        private float engagementRangeWorldUnits;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Scene Gizmo 등 디버그 표시용 플래그입니다.")]
        private bool showDebugGizmos = true;

        private float lastFireTimeSeconds = float.NegativeInfinity;

        private float runtimeAimErrorDegrees;

        private bool runtimeIsAimedWithinThreshold;

        private bool runtimeIsWithinEngagementRange;

        private bool runtimeHasTargetWorldPosition;

        private Vector3 runtimeTargetWorldPosition;

        private bool runtimeIsFireCooldownActive;

        public bool ShowDebugGizmos => showDebugGizmos;

        public float FireAngleThresholdDegrees => fireAngleThresholdDegrees;

        public float EngagementRangeWorldUnits => engagementRangeWorldUnits;

        public Transform MuzzleTransform => muzzlePoint;

        public float AimErrorDegrees => runtimeAimErrorDegrees;

        public bool IsAimedWithinThreshold => runtimeIsAimedWithinThreshold;

        public bool IsFireCooldownActive => runtimeIsFireCooldownActive;

        public bool IsWithinEngagementRange => runtimeIsWithinEngagementRange;

        /// <summary>
        /// 현재 프레임에서 추적할 타겟 Transform을 반환합니다. (파생 클래스에서 결정)
        /// </summary>
        protected abstract Transform GetCurrentTarget();

        /// <summary>
        /// 발사 가능 여부의 추가 조건(예: 탄약/스태미나)을 확장할 때 사용합니다.
        /// </summary>
        protected virtual bool CanFireAdditionalConditions(Transform currentTarget)
        {
            return true;
        }

        /// <summary>
        /// 발사체가 생성된 직후 후처리(이펙트/사운드 등)를 확장할 때 사용합니다.
        /// </summary>
        protected virtual void OnProjectileFired(GameObject projectileInstance)
        {
        }

        private void Update()
        {
            Transform currentTarget = GetCurrentTarget();
            runtimeHasTargetWorldPosition = false;
            runtimeIsWithinEngagementRange = false;
            runtimeIsAimedWithinThreshold = false;
            runtimeIsFireCooldownActive = false;
            runtimeAimErrorDegrees = 0f;

            if (currentTarget == null || yawPivot == null || pitchPivot == null || muzzlePoint == null || projectilePrefab == null)
            {
                return;
            }

            runtimeHasTargetWorldPosition = true;
            runtimeTargetWorldPosition = currentTarget.position;

            float distanceToTargetMeters = Vector3.Distance(muzzlePoint.position, currentTarget.position);
            runtimeIsWithinEngagementRange = engagementRangeWorldUnits <= 0f || distanceToTargetMeters <= engagementRangeWorldUnits;

            UpdateYawTowardsTarget(currentTarget);
            UpdatePitchTowardsTarget(currentTarget);

            RefreshAimDiagnostics(currentTarget);

            bool canAttemptFire = runtimeIsWithinEngagementRange && CanFireAdditionalConditions(currentTarget);
            bool cooldownBlocking = Time.time < lastFireTimeSeconds + fireIntervalSeconds;
            runtimeIsFireCooldownActive = runtimeIsAimedWithinThreshold && canAttemptFire && cooldownBlocking;

            if (canAttemptFire)
            {
                TryFireIfAimed(currentTarget);
            }
        }

        public bool TryGetTargetWorldPosition(out Vector3 targetWorldPosition)
        {
            if (!runtimeHasTargetWorldPosition)
            {
                targetWorldPosition = default;
                return false;
            }

            targetWorldPosition = runtimeTargetWorldPosition;
            return true;
        }

        private static float NormalizeAngle180(float angleDegrees)
        {
            float normalized = Mathf.Repeat(angleDegrees + 180f, 360f) - 180f;
            return normalized;
        }

        private void UpdateYawTowardsTarget(Transform currentTarget)
        {
            Vector3 pivotWorldPosition = yawPivot.position;
            Vector3 toTargetWorld = currentTarget.position - pivotWorldPosition;
            Vector3 flatDirection = new Vector3(toTargetWorld.x, 0f, toTargetWorld.z);

            if (flatDirection.sqrMagnitude < 1e-6f)
            {
                return;
            }

            flatDirection.Normalize();
            float desiredYawDegrees = Mathf.Atan2(flatDirection.x, flatDirection.z) * Mathf.Rad2Deg;
            float currentYawDegrees = yawPivot.eulerAngles.y;
            float nextYawDegrees = Mathf.MoveTowardsAngle(
                currentYawDegrees,
                desiredYawDegrees,
                yawSpeedDegreesPerSecond * Time.deltaTime);

            yawPivot.rotation = Quaternion.Euler(0f, nextYawDegrees, 0f);
        }

        private void UpdatePitchTowardsTarget(Transform currentTarget)
        {
            Vector3 toTargetWorld = (currentTarget.position - pitchPivot.position).normalized;
            Vector3 localDirection = Quaternion.Inverse(yawPivot.rotation) * toTargetWorld;

            float desiredPitchDegrees = Mathf.Atan2(localDirection.y, localDirection.z) * Mathf.Rad2Deg;
            desiredPitchDegrees = Mathf.Clamp(desiredPitchDegrees, minPitchDegrees, maxPitchDegrees);

            Vector3 localEuler = pitchPivot.localEulerAngles;
            float currentPitchDegrees = NormalizeAngle180(localEuler.x);
            float nextPitchDegrees = Mathf.MoveTowardsAngle(
                currentPitchDegrees,
                desiredPitchDegrees,
                pitchSpeedDegreesPerSecond * Time.deltaTime);

            pitchPivot.localRotation = Quaternion.Euler(nextPitchDegrees, 0f, 0f);
        }

        private void RefreshAimDiagnostics(Transform currentTarget)
        {
            Vector3 toTarget = currentTarget.position - muzzlePoint.position;
            if (toTarget.sqrMagnitude < 1e-8f)
            {
                runtimeAimErrorDegrees = 0f;
                runtimeIsAimedWithinThreshold = true;
                return;
            }

            Vector3 aimDirection = toTarget.normalized;
            float angleDegrees = Vector3.Angle(muzzlePoint.forward, aimDirection);
            runtimeAimErrorDegrees = angleDegrees;
            runtimeIsAimedWithinThreshold = angleDegrees <= fireAngleThresholdDegrees;
        }

        private void TryFireIfAimed(Transform currentTarget)
        {
            if (!runtimeIsAimedWithinThreshold)
            {
                return;
            }

            if (Time.time < lastFireTimeSeconds + fireIntervalSeconds)
            {
                return;
            }

            GameObject spawned = ProjectileSpawner.Spawn(
                projectilePrefab,
                muzzlePoint.position,
                muzzlePoint.rotation,
                projectileSpeed,
                projectileLifeTimeSeconds,
                projectileDamage,
                turretTeamId,
                projectileSpawnRoot);

            if (spawned == null)
            {
                return;
            }

            OnProjectileFired(spawned);
            lastFireTimeSeconds = Time.time;
        }
    }
}
