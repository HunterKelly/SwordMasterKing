using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SwordplayPrototype
{
    public class SwordplayArena : MonoBehaviour
    {
        [Header("Game mode")]
        public bool adventureMode;
        public BrokenGateLevel Level { get; private set; }
        public Transform PlayerTransform => player;
        public Camera PlayerCamera => view;
        public float SwingCharge => SwingModel.Charge(Time.time - lastAttack, recovery);
        float damageGraceUntil;

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
        void Start()
        {
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
            player = new GameObject("Swordsman").transform;
            player.SetParent(transform);
            player.position = new Vector3(0, .1f, -3);
            controller = player.gameObject.AddComponent<CharacterController>();
            controller.height = 1.9f; controller.radius = .32f; controller.center = Vector3.up * .95f;
            Shape("Torso", PrimitiveType.Capsule, new Vector3(0, 1.15f, 0), new Vector3(.65f, .5f, .4f), cloth, player);
            Shape("Helmet", PrimitiveType.Sphere, new Vector3(0, 1.83f, 0), Vector3.one * .43f, metal, player);
            Shape("Visor", PrimitiveType.Cube, new Vector3(0, 1.84f, .21f), new Vector3(.32f, .07f, .05f), floor, player);
            leftLeg = Shape("Left leg", PrimitiveType.Cube, new Vector3(-.18f, .4f, 0), new Vector3(.22f, .75f, .25f), stone, player).transform;
            rightLeg = Shape("Right leg", PrimitiveType.Cube, new Vector3(.18f, .4f, 0), new Vector3(.22f, .75f, .25f), stone, player).transform;
            Shape("Left arm", PrimitiveType.Capsule, new Vector3(-.43f, 1.2f, 0), new Vector3(.2f, .35f, .2f), metal, player);
            swordPivot = new GameObject("Procedural sword arm").transform;
            swordPivot.SetParent(player, false); swordPivot.localPosition = new Vector3(.4f, 1.2f, .05f);
            Shape("Sword arm", PrimitiveType.Cube, new Vector3(0, 0, .25f), new Vector3(.2f, .2f, .5f), metal, swordPivot);
            Shape("Grip", PrimitiveType.Cube, new Vector3(0, 0, .58f), new Vector3(.1f, .12f, .3f), stone, swordPivot);
            Shape("Guard", PrimitiveType.Cube, new Vector3(0, 0, .75f), new Vector3(.48f, .12f, .09f), gold, swordPivot);
            Shape("Blade", PrimitiveType.Cube, new Vector3(0, 0, 1.35f), new Vector3(.16f, .055f, 1.15f), metal, swordPivot);
            // Animate only the graphics. Movement collision and damage volumes stay upright.
            var graphics = new List<Transform>();
            foreach (Transform child in player) graphics.Add(child);
            visualRoot = new GameObject("Visuals only - roll pivot").transform;
            visualRoot.SetParent(player, false); visualRoot.localPosition = Vector3.up * .95f;
            foreach (var child in graphics) child.SetParent(visualRoot, true);
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
            view = new GameObject("Prototype camera").AddComponent<Camera>(); view.transform.SetParent(transform);
            view.fieldOfView = 65; view.nearClipPlane = .1f; view.backgroundColor = new Color(.025f, .04f, .07f); view.clearFlags = CameraClearFlags.SolidColor;
            view.gameObject.AddComponent<AudioListener>();
            var light = new GameObject("Arena sun").AddComponent<Light>(); light.transform.SetParent(transform);
            light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(50, -35, 0);
            RenderSettings.ambientLight = new Color(.5f, .55f, .65f);
            if (adventureMode)
            {
                Level = gameObject.AddComponent<BrokenGateLevel>();
                Level.Initialize(this);
            }
        }
        void SetMenu(bool value)
        {
            menu = value;
            Cursor.lockState = menu ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = menu;
        }
        void Update()
        {
            Vector2 move = Vector2.zero, look = Vector2.zero;
            bool click = false, escape = false, jump = false, roll = false, testLow = false, testHigh = false;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (keyboard != null)
            {
                move = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                escape = keyboard.escapeKey.wasPressedThisFrame;
                jump = keyboard.spaceKey.wasPressedThisFrame;
                roll = keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame;
                testLow = keyboard.lKey.wasPressedThisFrame; testHigh = keyboard.hKey.wasPressedThisFrame;
            }
            if (mouse != null) { look = mouse.delta.ReadValue() * .12f; click = mouse.leftButton.wasPressedThisFrame; }
#else
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2f;
            click = Input.GetMouseButtonDown(0); escape = Input.GetKeyDown(KeyCode.Escape);
            jump = Input.GetKeyDown(KeyCode.Space);
            roll = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            testLow = Input.GetKeyDown(KeyCode.L); testHigh = Input.GetKeyDown(KeyCode.H);
#endif
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
            while (attackHistory.Count > 0 && Time.time - attackHistory.Peek() > .5f) attackHistory.Dequeue();
            flurry = attackHistory.Count >= 3 && Time.time - lastAttack < .2f;
            float t = (Time.time - animationStart) / animationDuration;
            float angle = flurry ? Mathf.Sin((Time.time - animationStart) * 65) * 65 : Mathf.Lerp(-75, 75, Mathf.Clamp01(t));
            if (IsRolling) swordPivot.localRotation = Quaternion.Euler(-65, -30, 0);
            else if (t < 1 || flurry) swordPivot.localRotation = Quaternion.Euler(-10, angle * (swingIndex % 2 == 0 ? 1 : -1), 0);
            else swordPivot.localRotation = Quaternion.Slerp(swordPivot.localRotation, Quaternion.Euler(-25, 10, 0), Time.deltaTime * 12);
            for (int i = slashes.Count - 1; i >= 0; i--)
            {
                float life = (Time.time - slashes[i].born) / .18f;
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
        PlayerHurtbox CreateHurtbox(string label, bool lower, float height)
        {
            var go = new GameObject(label); go.transform.SetParent(player, false);
            go.transform.localPosition = Vector3.up * height;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true; box.size = new Vector3(.65f, .95f, .55f);
            var hurtbox = go.AddComponent<PlayerHurtbox>();
            hurtbox.Initialize(this, lower, box);
            return hurtbox;
        }
        void UpdateMovement(Vector2 input, bool jump, bool roll)
        {
            float dt = Time.deltaTime;
            bool grounded = controller.isGrounded;
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y).normalized;
            // Roll wins if jump and roll arrive together. Neither action is buffered.
            if (roll && grounded && !IsRolling && Time.time >= rollReadyAt)
            {
                rollDirection = direction.sqrMagnitude > .01f ? direction : player.forward;
                activeRollDuration = Mathf.Max(.1f, rollDuration);
                activeRollDistance = Mathf.Max(.1f, rollDistance);
                rollStartedAt = Time.time; rollEndsAt = Time.time + activeRollDuration;
                rollReadyAt = rollEndsAt + Mathf.Max(0, rollRecovery); rollTravelTime = 0;
                attackHistory.Clear(); animationStart = -100f;
                foreach (var slash in slashes) Destroy(slash.line.gameObject);
                slashes.Clear();
                if (Level != null) Level.PlaySound("roll");
            }
            if (grounded && verticalSpeed < 0) verticalSpeed = -2f;
            float gravity = Mathf.Max(1f, gravityStrength);
            if (jump && grounded && !IsRolling)
            {
                verticalSpeed = Mathf.Sqrt(2f * gravity * controller.height * Mathf.Max(.1f, jumpHeightFraction));
                if (Level != null) Level.PlaySound("jump");
            }

            Vector3 horizontal;
            if (IsRolling || rollTravelTime < activeRollDuration)
            {
                // Integrate only the remaining roll time, so distance doesn't vary with FPS.
                float step = Mathf.Min(dt, Mathf.Max(0, activeRollDuration - rollTravelTime));
                horizontal = rollDirection * (activeRollDistance / activeRollDuration) * step;
                rollTravelTime += step;
            }
            else horizontal = direction * moveSpeed * dt;
            float rise = verticalSpeed * dt - .5f * gravity * dt * dt;
            verticalSpeed -= gravity * dt;
            CollisionFlags flags = controller.Move(horizontal + Vector3.up * rise);
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
            if ((flags & CollisionFlags.Below) != 0 && verticalSpeed < 0) verticalSpeed = -2;
            airborne = !controller.isGrounded;

            if (IsRolling)
            {
                float progress = Mathf.Clamp01((Time.time - rollStartedAt) / activeRollDuration);
                float tuck = Mathf.Sin(progress * Mathf.PI);
                visualRoot.localPosition = Vector3.up * (.95f - .22f * tuck);
                visualRoot.localRotation = Quaternion.Inverse(player.rotation) * Quaternion.LookRotation(rollDirection) * Quaternion.Euler(360 * progress, 0, 0);
                visualRoot.localScale = Vector3.one * (1 - .2f * tuck);
                leftLeg.localRotation = rightLeg.localRotation = Quaternion.Euler(-50 * tuck, 0, 0);
            }
            else
            {
                visualRoot.localPosition = Vector3.up * .95f;
                visualRoot.localRotation = Quaternion.identity; visualRoot.localScale = Vector3.one;
                walkPhase += direction.magnitude * moveSpeed * dt * 2;
                float legAngle = airborne ? -30 : (direction.sqrMagnitude > .01f ? Mathf.Sin(walkPhase) * 25 : 0);
                leftLeg.localRotation = Quaternion.Euler(legAngle, 0, 0);
                rightLeg.localRotation = Quaternion.Euler(airborne ? -45 : -legAngle, 0, 0);
            }
        }
        void RefreshHurtboxes()
        {
            upperHurtbox.Volume.enabled = !IsRolling;
            lowerHurtbox.Volume.enabled = !IsRolling && !LowerBodyProtected;
        }
        public bool TryReceiveDamage(float amount, bool lowerBody)
        {
            if (adventureMode && (PlayerHealth <= 0 || Level == null || Level.InputBlocked || Time.time < damageGraceUntil)) return false;
            // The authoritative guard also rejects hits from a stale physics overlap.
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)
                || IsRolling || (lowerBody && LowerBodyProtected)) return false;
            PlayerHealth = Mathf.Max(0, PlayerHealth - amount);
            if (adventureMode)
            {
                damageGraceUntil = Time.time + .3f;
                Level.OnPlayerHurt();
            }
            return true;
        }
        void ProbeDamage(PlayerHurtbox hurtbox)
        {
            bool hit = hurtbox.TryHit(10);
            lastDefenseResult = (hurtbox.IsLowerBody ? "LOW" : "HIGH") + (hit ? " hit: -10 HP" : " hit: EVADED");
        }
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
        void LateUpdate()
        {
            Vector3 target = player.position + Vector3.up * 1.4f;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 direction = rotation * Vector3.back;
            float distance = 5.5f;
            // Ground, walls and dummies are the only obstacles; avoid hitting the player's collider.
            foreach (RaycastHit hit in Physics.SphereCastAll(target, .2f, direction, distance, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider != controller) distance = Mathf.Min(distance, Mathf.Max(.4f, hit.distance - .1f));
            view.transform.position = target + direction * distance;
            view.transform.LookAt(target);
            foreach (var d in dummies) d.label.transform.rotation = view.transform.rotation;
        }
        void OnGUI()
        {
            if (player == null || adventureMode) return;
            float scale = Mathf.Clamp(Screen.height / 800f, .7f, 1.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUILayout.BeginArea(new Rect(18, 18, 325, 660), GUI.skin.box);
            GUILayout.Label("SWORDPLAY / COMBAT LAB");
            GUILayout.Label("WASD move | Mouse aim | Click attack");
            GUILayout.Label("Space jump | Shift roll | Esc build controls");
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
        public void SetGameplayInput(bool enabledInput) { SetMenu(!enabledInput); }
        public void RestoreAt(Vector3 position)
        {
            controller.enabled = false; player.position = position; controller.enabled = true;
            PlayerHealth = 100; verticalSpeed = 0; damageGraceUntil = Time.time + 1f;
            yaw = 0; pitch = 22; player.rotation = Quaternion.identity;
            rollEndsAt = -100; rollReadyAt = 0; activeRollDuration = 0; rollTravelTime = 0;
            lastAttack = -100; animationStart = -100; attackHistory.Clear();
            airborne = false; visualRoot.localRotation = Quaternion.identity;
            visualRoot.localPosition = Vector3.up * .95f; visualRoot.localScale = Vector3.one;
            foreach (var slash in slashes) Destroy(slash.line.gameObject);
            slashes.Clear(); RefreshHurtboxes();
        }
        public bool Heal(float amount)
        {
            if (PlayerHealth <= 0 || PlayerHealth >= 100) return false;
            PlayerHealth = Mathf.Min(100, PlayerHealth + amount); return true;
        }
        void OnApplicationFocus(bool focus)
        {
            if (focus) return;
            if (adventureMode && Level != null) Level.Pause();
            else SetMenu(true);
        }
        void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            foreach (var material in materials) if (material != null) Destroy(material);
        }
    }
}
