using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    public partial class PlayerController
    {
        void Attack()
        {
            float elapsed = Time.time - lastAttack;
            if (elapsed + .00001f < 1f / SwingModel.MaxRate(speed)) { rejected++; return; }
            // Resolve gameplay NOW; no dependency on sword pose or animation completion.
            lastDamage = SwingModel.Damage(elapsed, power, recovery, speed);
            lastAttack = Time.time; totalAttacks++; attackHistory.Enqueue(Time.time);
            animationStart = Time.time; animationDuration = Mathf.Clamp(elapsed, .1f, .48f); swingIndex++;
            foreach (var d in dummies)
            {
                Vector3 delta = d.root.position - player.position; delta.y = 0;
                if (d.health <= 0 || delta.magnitude > reach || Vector3.Angle(player.forward, delta) > attackAngle * .5f) continue;
                float dealt = Mathf.Min(d.health, lastDamage); d.health -= dealt;
                d.flashUntil = Time.time + .08f;
                if (Feedback != null) Feedback.Impact(d.root.position + Vector3.up * 1.2f, delta, SwingModel.Charge(elapsed, recovery) >= .8f, d.health <= 0);
                damageHistory.Enqueue(new Vector2(Time.time, dealt));
                if (d.health <= 0) d.resetAt = Time.time + 1.5f;
            }
            if (Level != null)
            {
                Level.ResolvePlayerAttack(lastDamage, reach, attackAngle, SwingModel.Charge(elapsed, recovery));
                Level.PlaySound("swing");
            }
            EmitSlash(0);
            // Extra arcs are cosmetic only. Exactly one damage resolution per accepted press.
            if (attackHistory.Count >= 3) { EmitSlash(-.22f); EmitSlash(.22f); }
        }

        void EmitSlash(float offset)
        {
            var line = new GameObject("Cosmetic slash").AddComponent<LineRenderer>();
            line.transform.SetParent(transform); line.sharedMaterial = slashMaterial; line.positionCount = 18;
            line.useWorldSpace = true; line.widthMultiplier = .075f; line.numCapVertices = 3;
            for (int i = 0; i < 18; i++)
            {
                float angle = Mathf.Lerp(-attackAngle * .5f, attackAngle * .5f, i / 17f) * Mathf.Deg2Rad;
                Vector3 local = new Vector3(Mathf.Sin(angle) * 2, 1.25f + offset + Mathf.Sin(angle) * .22f * (swingIndex % 2 == 0 ? 1 : -1), Mathf.Cos(angle) * 2);
                line.SetPosition(i, player.TransformPoint(local));
            }
            slashes.Add(new Slash { line = line, born = Time.time });
        }
    }
}
