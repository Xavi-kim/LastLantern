using UnityEngine;

namespace LastLantern
{
    /// <summary>Движение игрока в 8 направлениях через Rigidbody2D без гравитации.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        /// <summary>Позиция игрока для врагов и капель (только чтение).</summary>
        public static Transform PlayerTransform { get; private set; }

        [SerializeField, Min(0f)] float moveSpeed = 5f;
        [SerializeField] SpriteRenderer spriteRenderer;

        Rigidbody2D body;
        Vector2 input;

        public float MoveSpeed => moveSpeed;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            PlayerTransform = transform;
        }

        void OnDestroy()
        {
            if (PlayerTransform == transform) PlayerTransform = null;
        }

        void Update()
        {
            input = GameInput.Move.ReadValue<Vector2>();
            if (input.sqrMagnitude > 1f) input.Normalize();

            // Поворачиваем спрайт по направлению движения
            if (spriteRenderer && Mathf.Abs(input.x) > 0.01f)
                spriteRenderer.flipX = input.x < 0f;
        }

        void FixedUpdate()
        {
            body.SetVelocity(input * moveSpeed);
        }

        public void AddMoveSpeed(float amount)
        {
            moveSpeed = Mathf.Max(0f, moveSpeed + amount);
        }
    }
}
