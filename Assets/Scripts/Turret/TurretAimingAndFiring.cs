using System;

namespace TurretDemo
{
    /// <summary>
    /// 레거시 호환용 타입입니다. 신규 작업은 <see cref="AutoTargetTurretController"/>를 사용하세요.
    /// </summary>
    [Obsolete("대신 AutoTargetTurretController 사용을 권장합니다.")]
    public sealed class TurretAimingAndFiring : AutoTargetTurretController
    {
    }
}
