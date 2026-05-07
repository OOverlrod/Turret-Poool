using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// 단일 Transform 타겟을 추적하는 기본 포탑 구현입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class AutoTargetTurretController : BaseTurretController
    {
        [Header("Target")]
        [SerializeField]
        [Tooltip("추적할 타겟 Transform.")]
        private Transform target;

        protected override Transform GetCurrentTarget()
        {
            return target;
        }
    }
}
