using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// TargetRailPivot을 Y축으로 회전시켜 자식 TargetDrone이 원형 궤도를 그리도록 합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TargetRailRotator : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Y축 회전 속도(도/초). Inspector에서 조정.")]
        private float rotationSpeedDegreesPerSecond = 20f;

        private void Update()
        {
            // 자식(TargetDrone)은 로컬 오프셋으로 원에서 떨어져 있으므로, 부모를 Y축으로 돌리면 원형 경로 이동이 됩니다.
            float deltaDegrees = rotationSpeedDegreesPerSecond * Time.deltaTime;
            transform.Rotate(0f, deltaDegrees, 0f, Space.Self);
        }
    }
}
