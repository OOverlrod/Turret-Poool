using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// 활성 EnemyTarget 목록에서 정책에 맞는 대상을 골라 추적하는 Turret 구현입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NearestEnemyTurretController : BaseTurretController
    {
        private enum TargetSelectionPolicy
        {
            Nearest = 0,
            First = 1,
            Random = 2,
        }

        [Header("Target Selection")]
        [SerializeField]
        private TargetSelectionPolicy selectionPolicy = TargetSelectionPolicy.Nearest;

        [SerializeField]
        [Tooltip("teamId가 이 값과 같으면 아군으로 간주하여 무시합니다.")]
        private int selfTeamId = -1;

        protected override Transform GetCurrentTarget()
        {
            switch (selectionPolicy)
            {
                case TargetSelectionPolicy.First:
                    return SelectFirst();
                case TargetSelectionPolicy.Random:
                    return SelectRandom();
                case TargetSelectionPolicy.Nearest:
                default:
                    return SelectNearest();
            }
        }

        private Transform SelectFirst()
        {
            var targets = EnemyTarget.ActiveTargets;
            for (int index = 0; index < targets.Count; index++)
            {
                EnemyTarget candidate = targets[index];
                if (!IsValidEnemy(candidate))
                {
                    continue;
                }

                if (!IsWithinRange(candidate.AimWorldPosition))
                {
                    continue;
                }

                return candidate.CachedTransform;
            }

            return null;
        }

        private Transform SelectRandom()
        {
            var targets = EnemyTarget.ActiveTargets;
            int validCount = 0;
            for (int index = 0; index < targets.Count; index++)
            {
                EnemyTarget candidate = targets[index];
                if (IsValidEnemy(candidate) && IsWithinRange(candidate.AimWorldPosition))
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return null;
            }

            int pickIndex = Random.Range(0, validCount);
            int seenValid = 0;
            for (int index = 0; index < targets.Count; index++)
            {
                EnemyTarget candidate = targets[index];
                if (!IsValidEnemy(candidate) || !IsWithinRange(candidate.AimWorldPosition))
                {
                    continue;
                }

                if (seenValid == pickIndex)
                {
                    return candidate.CachedTransform;
                }

                seenValid++;
            }

            return null;
        }

        private Transform SelectNearest()
        {
            var targets = EnemyTarget.ActiveTargets;
            float nearestDistanceSqr = float.PositiveInfinity;
            Transform nearest = null;

            for (int index = 0; index < targets.Count; index++)
            {
                EnemyTarget candidate = targets[index];
                if (!IsValidEnemy(candidate))
                {
                    continue;
                }

                Vector3 candidatePosition = candidate.AimWorldPosition;
                if (!IsWithinRange(candidatePosition))
                {
                    continue;
                }

                float distanceSqr = (candidatePosition - transform.position).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearest = candidate.CachedTransform;
                }
            }

            return nearest;
        }

        private bool IsValidEnemy(EnemyTarget candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            // selfTeamId가 음수면 팀 필터를 비활성화해 모든 Enemy를 타겟으로 간주합니다.
            if (selfTeamId < 0)
            {
                return true;
            }

            return candidate.TeamId != selfTeamId;
        }

        private bool IsWithinRange(Vector3 candidateWorldPosition)
        {
            float engagementRange = EngagementRangeWorldUnits;
            if (engagementRange <= 0f)
            {
                return true;
            }

            return (candidateWorldPosition - transform.position).sqrMagnitude <= engagementRange * engagementRange;
        }
    }
}
