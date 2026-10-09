using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    public partial class PlayerController
    {
        Material MakeMaterial(Color color, bool unlit = false)
        {
            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");
            var template = Resources.Load<Material>(unlit ? "BrokenGateUnlit" : "BrokenGateLit");
            var mat = template != null ? new Material(template) : new Material(shader);
            mat.color = color;
            materials.Add(mat);
            return mat;
        }

        GameObject Shape(string title, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, Transform parent = null, bool solid = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = title;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!solid) { var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider); }
            return go;
        }

        protected virtual void Start()
        {
            playerInput = GetComponent<PlayerInputReader>();
            if (playerInput == null) playerInput = gameObject.AddComponent<PlayerInputReader>();
            if (settings != null)
            {
                power = settings.power; recovery = settings.recovery; speed = settings.speed;
                maxStamina = settings.maxStamina; specialStaminaCost = settings.specialStaminaCost; sprintStaminaDrain = settings.sprintStaminaDrain;
                staminaRegeneration = settings.staminaRegeneration; staminaRegenerationDelay = settings.staminaRegenerationDelay;
                sprintMultiplier = settings.sprintMultiplier;
                fullChargeDamageMultiplier = settings.fullChargeDamageMultiplier;
                thrustHopHeight = settings.thrustHopHeight;
                thrustSlideDistance = settings.thrustSlideDistance; thrustSlideDuration = settings.thrustSlideDuration;
                thrustDuration = settings.thrustDuration; thrustDamageWindow = settings.thrustDamageWindow;
                specialAttackRecovery = settings.specialAttackRecovery; jumpingHeavyRecovery = settings.jumpingHeavyRecovery;
                chargedAttackRecovery = settings.chargedAttackRecovery;
                overheadReachMultiplier = settings.overheadReachMultiplier; thrustReachMultiplier = settings.thrustReachMultiplier;
                jumpingOverheadReachMultiplier = settings.jumpingOverheadReachMultiplier;
                strafeLeanAngle = settings.strafeLeanAngle; strafeLeanSpeed = settings.strafeLeanSpeed;
                moveSpeed = settings.moveSpeed; reach = settings.reach; attackAngle = settings.attackAngle;
                jumpHeightFraction = settings.jumpHeightFraction; rollDuration = settings.rollDuration;
                rollDistance = settings.rollDistance; rollRecovery = settings.rollRecovery;
                gravityStrength = settings.gravityStrength;
            }
            stamina = new StaminaPool(maxStamina,staminaRegeneration,staminaRegenerationDelay);
            var floor = MakeMaterial(new Color(.09f, .13f, .18f));
            var stone = MakeMaterial(new Color(.22f, .28f, .34f));
            var metal = MakeMaterial(new Color(.75f, .87f, .93f));
            var gold = MakeMaterial(new Color(.95f, .65f, .18f));
            var cloth = MakeMaterial(teal);
            slashMaterial = MakeMaterial(new Color(.4f, 1f, 1f), true);
            if (!adventureMode)
            {
            Shape("Arena floor", PrimitiveType.Cube, new Vector3(0, -.3f, 0), new Vector3(24, .6f, 24), floor, null, true);
            for (int i = 0; i < 4; i++)
            {
                var wall = Shape("Arena boundary", PrimitiveType.Cube, Quaternion.Euler(0, 90 * i, 0) * new Vector3(0, 1, 12), new Vector3(24, 2, .5f), stone, null, true);
                wall.transform.rotation = Quaternion.Euler(0, 90 * i, 0);
            }
            }
            var rig = playerPrefab != null ? Instantiate(playerPrefab, transform) : PlayerRig.CreateDefault(floor, stone, metal, gold, cloth);
            player = rig.transform; player.SetParent(transform, false);
            player.position = new Vector3(0, .1f, -3);
            controller = rig.controller; visualRoot = rig.visualRoot; swordPivot = rig.swordPivot;
            leftLeg = rig.leftLeg; rightLeg = rig.rightLeg; swordRestPosition = swordPivot.localPosition;
            lowerHurtbox = CreateHurtbox("Lower body damage hitbox", true, .475f);
            upperHurtbox = CreateHurtbox("Upper body damage hitbox", false, 1.425f);
            if (!adventureMode)
            foreach (Vector3 pos in new [] { new Vector3(0, 0, 1), new Vector3(-4, 0, 4), new Vector3(4, 0, 4) })
            {
                var root = new GameObject("Training dummy").transform; root.SetParent(transform); root.position = pos;
                Shape("Stand", PrimitiveType.Cylinder, new Vector3(0, .15f, 0), new Vector3(.85f, .15f, .85f), stone, root);
                var body = Shape("Target", PrimitiveType.Capsule, new Vector3(0, 1.1f, 0), new Vector3(.65f, .65f, .65f), MakeMaterial(new Color(.9f, .4f, .23f)), root, true);
                Shape("Crossbar", PrimitiveType.Cube, new Vector3(0, 1.25f, 0), new Vector3(1.3f, .15f, .15f), gold, root);
                var label = new GameObject("Health label").AddComponent<TextMesh>(); label.transform.SetParent(root, false);
                label.transform.localPosition = new Vector3(0, 2.3f, 0); label.characterSize = .07f; label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter;
                dummies.Add(new Dummy { root = root, body = body.GetComponent<Renderer>(), label = label });
            }
            view = new GameObject("Gameplay Camera").AddComponent<Camera>(); view.transform.SetParent(transform);
            view.fieldOfView = 65; view.nearClipPlane = .1f; view.backgroundColor = new Color(.025f, .04f, .07f); view.clearFlags = CameraClearFlags.SolidColor;
            view.gameObject.AddComponent<AudioListener>();
            cameraRig = view.gameObject.AddComponent<ThirdPersonCamera>();
            Feedback = gameObject.AddComponent<CombatFeedback>();
            Feedback.Initialize(cameraRig, levelDefinition != null ? levelDefinition.combatFeedback : null);
            var light = new GameObject("Arena sun").AddComponent<Light>(); light.transform.SetParent(transform);
            light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(50, -35, 0);
            RenderSettings.ambientLight = new Color(.5f, .55f, .65f);
            if (adventureMode)
            {
                Level = gameObject.AddComponent<BrokenGateLevel>();
                Level.Initialize(this);
            }
        }
        void UpdatePresentation()
        {
            while (attackHistory.Count > 0 && Time.time - attackHistory.Peek() > .5f) attackHistory.Dequeue();
            flurry = HorizontalFlurry;
            float t = (Time.time - animationStart) / animationDuration;
            float angle = flurry ? Mathf.Sin((Time.time - animationStart) * 65) * 65 : Mathf.Lerp(-75, 75, Mathf.Clamp01(t));
            if (IsRolling) swordPivot.localRotation = Quaternion.Euler(-65, -30, 0);
            else if (jumpStrikePending && !jumpDescending) swordPivot.localRotation = Quaternion.Euler(-135,0,0);
            else if (chargingAttack)
            {
                swordPivot.localRotation = chargeOverhead ? Quaternion.Euler(-135, 0, 0) : Quaternion.Euler(-15, -65, 0);
                if (chargeSprint && !chargeOverhead) swordPivot.localPosition = swordRestPosition + Vector3.back * .25f;
            }
            else if (t < 1)
            {
                float progress = Mathf.Clamp01(t);
                if (activeAttack == PlayerAttackKind.Overhead || activeAttack == PlayerAttackKind.ChargedOverhead || activeAttack == PlayerAttackKind.JumpingOverhead)
                    swordPivot.localRotation = Quaternion.Euler(activeAttack == PlayerAttackKind.Overhead && OverheadFlurry
                        ? -30 + Mathf.Sin((Time.time-animationStart)*65)*100 : Mathf.Lerp(-135,70,progress),0,0);
                else if (activeAttack == PlayerAttackKind.Thrust)
                {
                    swordPivot.localRotation = Quaternion.identity;
                    float extension = progress < .15f ? progress/.15f : progress < .85f ? 1 : (1-progress)/.15f;
                    swordPivot.localPosition = swordRestPosition + Vector3.forward * (Mathf.Clamp01(extension) * .9f);
                }
                else if (activeAttack == PlayerAttackKind.Spin) swordPivot.localRotation = Quaternion.Euler(-10, 70, 0);
                else swordPivot.localRotation = Quaternion.Euler(-10, angle * (swingIndex % 2 == 0 ? 1 : -1), 0);
            }
            else swordPivot.localRotation = Quaternion.Slerp(swordPivot.localRotation, Quaternion.Euler(-25, 10, 0), Time.deltaTime * 12);
            if (!chargingAttack && (activeAttack != PlayerAttackKind.Thrust || t >= 1)) swordPivot.localPosition = swordRestPosition;
            for (int i = slashes.Count - 1; i >= 0; i--)
            {
                float life = (Time.time - slashes[i].born) / slashes[i].duration;
                if(slashes[i].shockwave)
                {
                    float radius=Mathf.Lerp(.15f,3.5f,Mathf.Clamp01(life));
                    for(int j=0;j<slashes[i].line.positionCount;j++)
                    {
                        float a=j/(float)(slashes[i].line.positionCount-1)*Mathf.PI*2;
                        slashes[i].line.SetPosition(j,slashes[i].center+new Vector3(Mathf.Sin(a)*radius,.06f,Mathf.Cos(a)*radius));
                    }
                }
                if (life >= 1) { Destroy(slashes[i].line.gameObject); slashes.RemoveAt(i); }
                else slashes[i].line.widthMultiplier = .075f * (1 - life);
            }
            foreach (var d in dummies)
            {
                if (d.health <= 0 && Time.time >= d.resetAt) d.health = 250;
                d.body.sharedMaterial.color = Time.time < d.flashUntil ? Color.white : new Color(.9f, .4f, .23f);
                d.label.text = d.health <= 0 ? "RESETTING" : Mathf.CeilToInt(d.health) + " / 250";
            }
            while (damageHistory.Count > 0 && Time.time - damageHistory.Peek().x > 5) damageHistory.Dequeue();
        }
    }
}
