using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// 터렛의 조준 오차·쿨다운·사거리 제한을 Scene Gizmo로 시각화합니다. (SRP: 표시만 담당)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TurretGizmoVisualizer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("비워두면 같은 GameObject에서 BaseTurretController를 찾습니다.")]
        private BaseTurretController turretController;

        [Header("Gizmo Display")]
        [SerializeField]
        [Tooltip("선택 시에만 그리면 Scene 뷰가 더 깔끔합니다.")]
        private bool drawOnlyWhenSelected = true;

        [SerializeField]
        private bool showAimCone = true;

        [SerializeField]
        private bool showRays = true;

        [SerializeField]
        private bool showEngagementSphere;

        [SerializeField]
        private bool showTargetMarker = true;

        [SerializeField]
        [Min(0.05f)]
        private float targetMarkerRadius = 0.3f;

        [SerializeField]
        [Min(3)]
        private int aimConeSegments = 32;

        [SerializeField]
        [Min(0.1f)]
        private float gizmoRayLengthWorldUnits = 8f;

        [SerializeField]
        [Min(0.1f)]
        private float aimConeDepthWorldUnits = 4f;

        private void OnDrawGizmos()
        {
            if (!drawOnlyWhenSelected)
            {
                DrawGizmosInternal();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (drawOnlyWhenSelected)
            {
                DrawGizmosInternal();
            }
        }

        private void Reset()
        {
            turretController = GetComponent<BaseTurretController>();
        }

        private void DrawGizmosInternal()
        {
            BaseTurretController resolved = turretController != null ? turretController : GetComponent<BaseTurretController>();
            if (resolved == null || !resolved.ShowDebugGizmos)
            {
                return;
            }

            Transform muzzle = resolved.MuzzleTransform;
            if (muzzle == null)
            {
                return;
            }

            bool hasTarget = resolved.TryGetTargetWorldPosition(out Vector3 targetWorldPosition);
            Color stateColor = ResolveStateColor(resolved, hasTarget);
            Gizmos.color = stateColor;

            if (showEngagementSphere && resolved.EngagementRangeWorldUnits > 0f)
            {
                Gizmos.DrawWireSphere(muzzle.position, resolved.EngagementRangeWorldUnits);
            }

            if (showRays)
            {
                Gizmos.DrawRay(muzzle.position, muzzle.forward * gizmoRayLengthWorldUnits);
                if (hasTarget)
                {
                    Vector3 toTarget = targetWorldPosition - muzzle.position;
                    if (toTarget.sqrMagnitude > 1e-8f)
                    {
                        Gizmos.DrawRay(muzzle.position, toTarget.normalized * gizmoRayLengthWorldUnits);
                    }
                }
            }

            if (showTargetMarker && hasTarget)
            {
                Gizmos.DrawWireSphere(targetWorldPosition, targetMarkerRadius);
            }

            if (showAimCone)
            {
                DrawAimCone(
                    muzzle,
                    resolved.FireAngleThresholdDegrees,
                    aimConeDepthWorldUnits,
                    aimConeSegments,
                    gizmoRayLengthWorldUnits);
            }
        }

        /// <summary>
        /// 조준 허용 각 기준으로 원뿔 단면(원)과 경계선을 근사합니다.
        /// </summary>
        private static void DrawAimCone(
            Transform muzzle,
            float halfAngleDegrees,
            float depthWorldUnits,
            int segments,
            float boundaryRayLengthWorldUnits)
        {
            if (muzzle == null || segments < 3)
            {
                return;
            }

            Vector3 forward = muzzle.forward.normalized;
            Vector3 referenceUp = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Cross(forward, referenceUp).normalized;
            Vector3 up = Vector3.Cross(right, forward).normalized;

            float halfAngleRadians = halfAngleDegrees * Mathf.Deg2Rad;
            float radius = depthWorldUnits * Mathf.Tan(halfAngleRadians);
            Vector3 coneCenter = muzzle.position + forward * depthWorldUnits;

            Vector3 previousPoint = coneCenter + right * radius;
            for (int segmentIndex = 1; segmentIndex <= segments; segmentIndex++)
            {
                float t = (segmentIndex / (float)segments) * Mathf.PI * 2f;
                Vector3 nextPoint = coneCenter + (Mathf.Cos(t) * right + Mathf.Sin(t) * up) * radius;
                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }

            Vector3 axis = Vector3.Cross(forward, referenceUp).normalized;
            if (axis.sqrMagnitude < 1e-6f)
            {
                return;
            }

            Vector3 boundaryA = Quaternion.AngleAxis(-halfAngleDegrees, axis) * forward;
            Vector3 boundaryB = Quaternion.AngleAxis(halfAngleDegrees, axis) * forward;
            Gizmos.DrawRay(muzzle.position, boundaryA * boundaryRayLengthWorldUnits);
            Gizmos.DrawRay(muzzle.position, boundaryB * boundaryRayLengthWorldUnits);
        }

        private static Color ResolveStateColor(ITurretAimDebugState state, bool hasTarget)
        {
            if (!hasTarget)
            {
                return new Color(0.6f, 0.6f, 0.6f, 0.9f);
            }

            if (!state.IsWithinEngagementRange)
            {
                return new Color(1f, 0.85f, 0.2f, 0.95f);
            }

            if (state.IsAimedWithinThreshold && state.IsFireCooldownActive)
            {
                return new Color(1f, 0.25f, 0.25f, 0.95f);
            }

            if (state.IsAimedWithinThreshold)
            {
                return new Color(0.2f, 0.95f, 0.35f, 0.95f);
            }

            return new Color(1f, 0.85f, 0.2f, 0.95f);
        }
    }
}
