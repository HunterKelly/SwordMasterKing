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
        public float SwingCharge => SwingModel.Charge(Time.time - lastAttack, recovery);
        float damageGraceUntil;
        [Header("Project assets")]
        [SerializeField] PlayerSettings settings;
        [SerializeField] PlayerRig playerPrefab;
        [SerializeField] BrokenGateWorld authoredWorld;
        [SerializeField] LevelDefinition levelDefinition;
        PlayerInputReader playerInput;
        ThirdPersonCamera cameraRig;
        public PlayerInputFrame InputFrame => playerInput.Read();
        public BrokenGateWorld AuthoredWorld => authoredWorld;
        public LevelDefinition LevelDefinition => levelDefinition;

        [Range(0, 10)] public int power = 4;
        [Range(0, 10)] public int recovery = 4;
        [Range(0, 10)] public int speed = 4;
        public float moveSpeed = 5f;
        public float reach = 2.8f;
        [Range(30, 180)] public float attackAngle = 110f;

        [Header("Jump and roll")]
        [Range(.1f, 1.5f)] public float jumpHeightFraction = .5f;
        [Min(.1f)] public float rollDuration = .55f;
        [Min(.1f)] public float rollDistance = 3.6f;
        [Min(0f)] public float rollRecovery = .18f;
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
            public float born;
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
            menu = value;
            Cursor.lockState = menu ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = menu;
        }
        protected virtual void Update()
        {
            var input = playerInput.Read();
            Vector2 move = input.Move, look = input.Look;
            bool click = input.Attack, escape = input.Pause, jump = input.Jump, roll = input.Roll;
            bool testLow = input.TestLow, testHigh = input.TestHigh;
            if (escape)
            {
                if (adventureMode && Level != null) Level.TogglePause();
                else SetMenu(!menu);
            }
            if (adventureMode && (Level == null || Level.InputBlocked)) return;
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
                if (click && !IsRolling) Attack();
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
        public void SetGameplayInput(bool enabledInput) { SetMenu(!enabledInput); }

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
