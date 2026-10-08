using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    // Owns only temporary impact freezes. Journey screen changes explicitly cancel them.
    [DefaultExecutionOrder(-50)]
    public sealed class CombatFeedback : MonoBehaviour
    {
        sealed class Spark
        {
            public LineRenderer line;
            public Vector3 position, velocity;
            public float life, duration;
        }
        readonly List<Spark> sparks = new List<Spark>();
        ThirdPersonCamera cameraRig;
        CombatFeedbackSettings settings;
        Material sparkMaterial;
        bool ownsFreeze;
        float freezeUntil, freezeReadyAt, previousTimeScale;
        public bool ImpactPaused => ownsFreeze;
        public void Initialize(ThirdPersonCamera camera, CombatFeedbackSettings tuning)
        {
            cameraRig = camera; settings = tuning ?? new CombatFeedbackSettings();
            var template = Resources.Load<Material>("BrokenGateUnlit");
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (template != null || shader != null)
            {
                sparkMaterial = template != null ? new Material(template) : new Material(shader);
                sparkMaterial.color = new Color(1f, .72f, .22f);
            }
        }
        public void Impact(Vector3 position, Vector3 away, bool heavy, bool killed)
        {
            if (settings == null) return;
            if (settings.cameraShake && cameraRig != null)
                cameraRig.AddImpact((heavy ? .65f : .25f) * settings.shakeStrength, killed ? .18f : .12f);
            if (settings.sparks && sparkMaterial != null) EmitSparks(position, away, heavy || killed);
            if (!settings.hitStop || ownsFreeze || Time.timeScale != 1f || Time.unscaledTime < freezeReadyAt) return;
            float duration = Mathf.Clamp(heavy ? settings.heavyHitPause : settings.lightHitPause, 0, .08f);
            if (duration <= 0) return;
            previousTimeScale = Time.timeScale;
            ownsFreeze = true; freezeUntil = Time.unscaledTime + duration;
            freezeReadyAt = freezeUntil + .1f;
            Time.timeScale = 0;
        }
        void EmitSparks(Vector3 position, Vector3 away, bool strong)
        {
            int count = strong ? 10 : 5;
            for (int i=0; i<count; i++)
            {
                var line = new GameObject("Impact spark").AddComponent<LineRenderer>();
                line.transform.SetParent(transform, false); line.sharedMaterial = sparkMaterial;
                line.useWorldSpace = true; line.positionCount = 2; line.widthMultiplier = strong ? .025f : .018f;
                var direction = (Random.onUnitSphere + Vector3.up * .5f + away.normalized * .4f).normalized;
                var spark = new Spark { line=line, position=position, velocity=direction * Random.Range(2f, 4.5f), duration=Random.Range(.12f, .24f) };
                line.SetPosition(0, position); line.SetPosition(1, position + direction * .1f);
                sparks.Add(spark);
            }
        }
        void Update()
        {
            if (ownsFreeze && Time.unscaledTime >= freezeUntil) CancelImpactPause();
            float dt = Time.deltaTime; // Effects pause with menus and hit stop.
            for (int i=sparks.Count-1; i>=0; i--)
            {
                var spark = sparks[i]; spark.life += dt;
                if (spark.life >= spark.duration) { Destroy(spark.line.gameObject); sparks.RemoveAt(i); continue; }
                spark.velocity += Vector3.down * 8f * dt; spark.position += spark.velocity * dt;
                spark.line.SetPosition(0, spark.position);
                spark.line.SetPosition(1, spark.position - spark.velocity.normalized * .13f);
                spark.line.widthMultiplier = .025f * (1f - spark.life / spark.duration);
            }
        }
        public void CancelImpactPause()
        {
            if (!ownsFreeze) return;
            ownsFreeze = false; Time.timeScale = previousTimeScale;
        }
        void OnDisable() { CancelImpactPause(); }
        void OnDestroy()
        {
            CancelImpactPause();
            foreach (var spark in sparks) if (spark.line != null) Destroy(spark.line.gameObject);
            if (sparkMaterial != null) Destroy(sparkMaterial);
        }
    }
}
