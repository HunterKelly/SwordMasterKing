using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    public partial class PlayerController
    {
        protected virtual void OnGUI()
        {
            if (player == null || adventureMode) return;
            float scale = Mathf.Clamp(Screen.height / 800f, .7f, 1.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUILayout.BeginArea(new Rect(18, 18, 325, 660), GUI.skin.box);
            GUILayout.Label("SWORDPLAY / COMBAT LAB");
            GUILayout.Label("WASD move | LMB slash / thrust | RMB overhead / jump");
            GUILayout.Label("Hold click charge (2s) | Shift hold sprint / tap roll | Esc build controls");
            GUILayout.Label(IsRolling ? "ROLLING - INVINCIBLE" : (airborne ? "AIRBORNE - lower hitbox OFF" : "GROUNDED - both hitboxes ON"));
            GUILayout.Label("HP: " + PlayerHealth.ToString("F0") + " / 100  |  Roll: " + (Time.time >= rollReadyAt ? "READY" : "recovering"));
            GUILayout.Label(lastDefenseResult);
            float charge = SwingModel.Charge(Time.time - lastAttack, recovery);
            GUILayout.Label("Swing power: " + Mathf.RoundToInt(charge * 100) + "%");
            Rect bar = GUILayoutUtility.GetRect(290, 14);
            GUI.color = Color.gray; GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = teal; GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * charge, bar.height), Texture2D.whiteTexture); GUI.color = Color.white;
            GUILayout.Label("Next hit: " + SwingModel.Damage(Time.time-lastAttack, power, recovery, speed).ToString("F1") + " | Last: " + lastDamage.ToString("F1"));
            GUILayout.Label("Cap: " + SwingModel.MaxRate(speed).ToString("F1") + " attacks/sec");
            float damage = 0; foreach (var item in damageHistory) damage += item.y;
            GUILayout.Label("Damage/sec, last 5s: " + (damage / 5).ToString("F1"));
            GUILayout.Label("Accepted: " + totalAttacks + " | Over cap: " + rejected);
            GUILayout.Label(flurry ? "FLURRY" : "DELIBERATE / FLOW");
            if (menu)
            {
                GUILayout.Space(8); GUILayout.Label("BUILD LAB — 12 points, free respec");
                StatControl("Power", ref power); StatControl("Recovery", ref recovery); StatControl("Speed", ref speed);
                GUILayout.Label("Unspent: " + (12 - power - recovery - speed));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Heavy")) { power = 10; recovery = 2; speed = 0; }
                if (GUILayout.Button("Balanced")) { power = 4; recovery = 4; speed = 4; }
                if (GUILayout.Button("Fast")) { power = 0; recovery = 2; speed = 10; }
                GUILayout.EndHorizontal();
                showHurtboxes = GUILayout.Toggle(showHurtboxes, "Show damage hitboxes (Scene gizmos)");
                if (GUILayout.Button("Reset test health")) PlayerHealth = 100f;
                if (GUILayout.Button("PLAY")) SetMenu(false);
            }
            GUILayout.EndArea();
            GUI.matrix = Matrix4x4.identity;
            if (!menu) GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+");
        }

        void StatControl(string label, ref int value)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(label + "  " + value, GUILayout.Width(175));
            if (GUILayout.Button("-", GUILayout.Width(40)) && value > 0) value--;
            if (GUILayout.Button("+", GUILayout.Width(40)) && value < 10 && power + recovery + speed < 12) value++;
            GUILayout.EndHorizontal();
        }

        void ProbeDamage(PlayerHurtbox hurtbox)
        {
            bool hit = hurtbox.TryHit(10);
            lastDefenseResult = (hurtbox.IsLowerBody ? "LOW" : "HIGH") + (hit ? " hit: -10 HP" : " hit: EVADED");
        }
    }
}
