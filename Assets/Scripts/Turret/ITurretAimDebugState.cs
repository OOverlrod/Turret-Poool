using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// 터렛 조준·발사 제어 상태를 외부(Gizmo 등)에서 읽기 위한 최소 인터페이스입니다.
    /// </summary>
    public interface ITurretAimDebugState
    {
        /// <summary>Inspector에서 Gizmo 표시 여부.</summary>
        bool ShowDebugGizmos { get; }

        /// <summary>조준 허용 각(도).</summary>
        float FireAngleThresholdDegrees { get; }

        /// <summary>0 이하면 무제한, 초과 시 Muzzle 기준 거리 제한.</summary>
        float EngagementRangeWorldUnits { get; }

        Transform MuzzleTransform { get; }

        bool TryGetTargetWorldPosition(out Vector3 targetWorldPosition);

        float AimErrorDegrees { get; }

        bool IsAimedWithinThreshold { get; }

        bool IsFireCooldownActive { get; }

        bool IsWithinEngagementRange { get; }
    }
}
