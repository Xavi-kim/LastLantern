using System;
using UnityEngine;

namespace LastLantern
{
    /// <summary>
    /// Капля масла: притягивается к игроку, при подборе увеличивает радиус фонаря и даёт опыт.
    /// Работает без физики — простая проверка расстояния, это дёшево даже для сотен капель.
    /// </summary>
    public class OilDrop : MonoBehaviour
    {
        [SerializeField] Transform visual;
        [SerializeField, Min(0f)] float magnetRadius = 3f;
        [SerializeField, Min(0f)] float magnetSpeed = 11f;
        [SerializeField, Min(0.05f)] float collectRadius = 0.55f;
        [SerializeField, Min(1f)] float lifetime = 45f;

        int xp;
        float oil;
        float life;
        Action<OilDrop> release;
        bool active;
        float bobPhase;

        public void Init(int xpReward, float oilAmount, Action<OilDrop> releaseCallback)
        {
            xp = xpReward;
            oil = oilAmount;
            release = releaseCallback;
            life = lifetime;
            active = true;
            bobPhase = UnityEngine.Random.value * 10f;
        }

        void Update()
        {
            if (!active) return;

            life -= Time.deltaTime;
            if (life <= 0f) { Despawn(); return; }

            if (visual)
            {
                float s = 1f + Mathf.Sin((Time.time + bobPhase) * 6f) * 0.1f;
                visual.localScale = new Vector3(s, s, 1f);
            }

            Transform target = PlayerController.PlayerTransform;
            if (!target) return;

            Vector3 toPlayer = target.position - transform.position;
            toPlayer.z = 0f;
            float distSqr = toPlayer.sqrMagnitude;

            if (distSqr <= collectRadius * collectRadius)
                Collect();
            else if (distSqr <= magnetRadius * magnetRadius)
                transform.position += toPlayer.normalized * (magnetSpeed * Time.deltaTime);
        }

        void Collect()
        {
            if (LanternLight.Current) LanternLight.Current.AddOil(oil);
            GameEvents.RaiseOilCollected(xp);
            Despawn();
        }

        public void Despawn()
        {
            if (!active) return;
            active = false;
            var callback = release;
            release = null;
            if (callback != null) callback(this);
            else gameObject.SetActive(false);
        }
    }
}
