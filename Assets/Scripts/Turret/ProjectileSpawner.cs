using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// 발사체 프리팹을 생성하고 <see cref="ProjectileMover"/> 초기화까지 담당합니다. (SRP)
    /// </summary>
    public static class ProjectileSpawner
    {
        /// <summary>
        /// Muzzle 위치·회전에 맞춰 발사체를 생성하고 등속 이동 파라미터를 적용합니다.
        /// </summary>
        /// <param name="projectilePrefab">발사체 프리팹.</param>
        /// <param name="spawnPosition">월드 생성 위치.</param>
        /// <param name="spawnRotation">월드 생성 회전(전진 방향).</param>
        /// <param name="speedUnitsPerSecond">전진 속도.</param>
        /// <param name="lifeTimeSeconds">수명(초).</param>
        /// <param name="parent">정리용 부모(없으면 null).</param>
        /// <returns>생성된 인스턴스(실패 시 null).</returns>
        public static GameObject Spawn(
            GameObject projectilePrefab,
            Vector3 spawnPosition,
            Quaternion spawnRotation,
            float speedUnitsPerSecond,
            float lifeTimeSeconds,
            float damageAmount,
            int shooterTeamId,
            Transform parent)
        {
            if (projectilePrefab == null)
            {
                return null;
            }

            GameObject instance = Object.Instantiate(projectilePrefab, spawnPosition, spawnRotation, parent);
            ProjectileMover mover = instance.GetComponent<ProjectileMover>();
            if (mover != null)
            {
                mover.Initialize(speedUnitsPerSecond, lifeTimeSeconds, damageAmount, shooterTeamId);
            }

            return instance;
        }
    }
}
