using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    public partial class PlayerController : MonoBehaviour
    {
        [Header("Game mode")]
        public bool adventureMode;
        public BrokenGateLevel Level { get; private set; }
        public Transform PlayerTransform => player;
        public Camera PlayerCamera => view;
        public float SwingCharge => chargingAttack ? ChargeFraction : SwingModel.Charge(Time.time - lastAttack, recovery);
        public bool IsSprinting { get; private set; }
        public bool IsChargingAttack => chargingAttack;
        StaminaPool stamina;
        public float PlayerStamina => stamina != null ? stamina.Current : maxStamina;
        public float StaminaFraction => stamina != null ? stamina.Current/stamina.Maximum : 1;
        public bool StaminaShortage => Time.time < staminaWarningUntil;
        float staminaWarningUntil;
        float damageGraceUntil;
        readonly AttackInputBuffer attackBuffer = new AttackInputBuffer();
        [Header("Project assets")]
        [SerializeField] PlayerSettings settings;
        [SerializeField] PlayerRig playerPrefab;
        [SerializeField] BrokenGateWorld authoredWorld;
        [SerializeField] LevelDefinition levelDefinition;
        PlayerInputReader playerInput;
        ThirdPersonCamera cameraRig;
        public CombatFeedback Feedback { get; private set; }
        public PlayerInputFrame InputFrame => playerInput.Read();
        public BrokenGateWorld AuthoredWorld => authoredWorld;
        public LevelDefinition LevelDefinition => levelDefinition;

        [Range(0, 10)] public int power = 4;
        [Range(0, 10)] public int recovery = 4;
        [Range(0, 10)] public int speed = 4;
        public float moveSpeed = 6f;
        [Header("Strafe rotation (visuals only)")]
        [InspectorName("Strafe Turn Angle"), Range(0, 60)] public float strafeLeanAngle = 45f;
        [InspectorName("Strafe Turn Speed"), Min(1)] public float strafeLeanSpeed = 360f;
        float strafeLean;
        public float reach = 2.8f;
        [Range(30, 180)] public float attackAngle = 110f;

        [Header("Stamina")]
        [Min(1)] public float maxStamina = 100f;
        [Min(0)] public float specialStaminaCost = 35f;
        [Min(0)] public float sprintStaminaDrain = 8f;
        [Min(0)] public float staminaRegeneration = 60f;
        [Min(0)] public float staminaRegenerationDelay = .2f;
        [Header("Sprint and charged attacks")]
        [Min(1)] public float sprintMultiplier = 1.6f;
        [Min(1)] public float fullChargeDamageMultiplier = 2f;
        [Header("Attack windows and recovery")]
        [Range(0, .4f)] public float thrustHopHeight = .12f;
        [Min(0)] public float thrustSlideDistance = 1.2f;
        [Min(.05f)] public float thrustSlideDuration = .2f;
        [Min(.1f)] public float thrustDuration = .55f;
        [Min(.1f)] public float thrustDamageWindow = .45f;
        [Min(0)] public float specialAttackRecovery = .15f;
        [Min(0)] public float jumpingHeavyRecovery = .2f;
        [Min(0)] public float chargedAttackRecovery = .12f;
        [Header("Special attack reach multipliers")]
        [Min(1)] public float overheadReachMultiplier = 1.6f;
        [Min(1)] public float thrustReachMultiplier = 2.1f;
        [Min(1)] public float jumpingOverheadReachMultiplier = 1.6f;
        [Header("Jump and roll")]
        [Range(.1f, 1.5f)] public float jumpHeightFraction = .5f;
        [Min(.1f)] public float rollDuration = .45f;
        [Min(.1f)] public float rollDistance = 3.6f;
        [Min(0f)] public float rollRecovery = .1f;
        [Min(1f)] public float gravityStrength = 20f;
        public bool showHurtboxes;

        public bool IsRolling => Time.time < rollEndsAt;
        public bool LowerBodyProtected => airborne;
        public float PlayerHealth { get; private set; } = 100f;
        float rollStartedAt = -100f, rollEndsAt = -100f, rollReadyAt;
        float activeRollDuration, activeRollDistance, rollTravelTime;
        Vector3 rollDirection;
        bool airborne;
        Transform visualRoot;
        PlayerHurtbox upperHurtbox, lowerHurtbox;
        string lastDefenseResult = "L: test low hit | H: test high hit";

        class Dummy
        {
            public Transform root;
            public Renderer body;
            public TextMesh label;
            public float health = 250f, resetAt, flashUntil;
        }
        class Slash
        {
            public LineRenderer line;
            public float born, duration = .18f;
            public bool shockwave;
            public Vector3 center;
        }
        readonly List<Dummy> dummies = new List<Dummy>();
        readonly List<Slash> slashes = new List<Slash>();
        readonly Queue<Vector2> damageHistory = new Queue<Vector2>();
        readonly Queue<float> attackHistory = new Queue<float>();
        readonly List<Material> materials = new List<Material>();
        Transform player, swordPivot, leftLeg, rightLeg;
        CharacterController controller;
        Camera view;
        Material slashMaterial;
        float yaw, pitch = 22, verticalSpeed, lastAttack = -100, lastDamage;
        float animationStart = -100, animationDuration = .4f;
        int swingIndex, totalAttacks, rejected;
        bool menu = true, flurry;
        float walkPhase;
        Color teal = new Color(.12f, .8f, .8f);

        void SetMenu(bool value)
        {
            if (value) CancelCombatInput();
            menu = value;
            Cursor.lockState = menu ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = menu;
        }
        protected virtual void Update()
        {
            var input = playerInput.Read();
            Vector2 move = input.Move, look = input.Look;
            bool escape = input.Pause, jump = input.Jump;
            bool testLow = input.TestLow, testHigh = input.TestHigh;
            if (escape)
            {
                if (adventureMode && Level != null) Level.TogglePause();
                else SetMenu(!menu);
            }
            if (adventureMode && (Level == null || Level.InputBlocked)) { CancelCombatInput(); return; }
            UpdateSprint(!menu && input.ShiftHeld, move);
            bool roll = input.Roll;
            if (menu || IsRolling) CancelAttackInput();
            else ReadAttackInput(input);
            if (Feedback != null && Feedback.ImpactPaused) return;
            if(stamina!=null)
            {
                if(!menu && IsSprinting) stamina.Drain(Mathf.Max(0,sprintStaminaDrain)*Time.deltaTime);
                else stamina.Tick(Time.deltaTime,!menu && !chargingAttack && !jumpStrikePending && Time.time>=specialPoseUntil);
                if(stamina.Current<=0) { IsSprinting=false; sprintExhausted=true; }
            }
            if (!menu)
            {
                yaw += look.x; pitch = Mathf.Clamp(pitch - look.y, 5, 65);
                if (!IsRolling) player.rotation = Quaternion.Euler(0, yaw, 0);
            }
            // Gravity and an active roll continue even with the build panel open.
            UpdateMovement(menu ? Vector2.zero : move, !menu && jump, !menu && roll);
            RefreshHurtboxes();
            if (!menu)
            {
                if (IsRolling) { CancelAttackInput(); jumpStrikePending = false; thrustWindow.Cancel(); }
                else
                {
                    UpdateJumpStrike();
                    UpdateAttackWindows();
                    if (!jumpStrikePending && Time.time >= attackReadyAt && attackBuffer.Consume(Time.unscaledTime,
                        Time.time - lastAttack + .00001f >= 1f / SwingModel.MaxRate(speed))) Attack();
                }
                if (!adventureMode && testLow) ProbeDamage(lowerHurtbox);
                if (!adventureMode && testHigh) ProbeDamage(upperHurtbox);
            }
            UpdatePresentation();
        }

        protected virtual void LateUpdate()
        {
            if (player == null || cameraRig == null) return;
            cameraRig.Follow(player, controller, yaw, pitch);
            foreach (var d in dummies) d.label.transform.rotation = view.transform.rotation;
        }
        public void SetGameplayInput(bool enabledInput) { CancelCombatInput(); SetMenu(!enabledInput); }

        protected virtual void OnApplicationFocus(bool focus)
        {
            if (focus) return;
            if (adventureMode && Level != null) Level.Pause();
            else SetMenu(true);
        }
        protected virtual void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            foreach (var material in materials) if (material != null) Destroy(material);
        }
    }
}
