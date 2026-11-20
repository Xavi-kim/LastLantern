using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// Тень, которая просто идёт к игроку. Один скрипт на бегуна и тяжеловеса —
    /// разница только в EnemyData (здоровье, урон, скорость).
    /// </summary>
    public class EnemyChaser : EnemyBase
    {
        protected override Vector2 DesiredVelocity()
        {
            Vector2 toPlayer = PlayerPosition - body.position;
            if (toPlayer.sqrMagnitude < 0.0001f) return Vector2.zero;
            return toPlayer.normalized * CurrentSpeed();
        }
    }
}
