using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// Совместимость Unity 2022 и Unity 6: в Unity 6 Rigidbody2D.velocity переименован в linearVelocity.
    /// </summary>
    public static class PhysicsCompat
    {
        public static void SetVelocity(this Rigidbody2D body, Vector2 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }
    }
}
