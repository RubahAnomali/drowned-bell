using System.Collections.Generic;
using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    /// <summary>
    /// The only MonoBehaviour. Holds the tuning (shown in the Inspector; edits apply live), runs the simulation at
    /// a fixed 60 Hz with hit-stop, follows the diver with a room-clamped camera, and draws the HUD.
    /// All game rules live in DrownedBell.Core.World.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        [Header("Feel: every value applies live while playing")]
        public MovementTuning movement = new MovementTuning();
        public CombatTuning combat = new CombatTuning();
        public EnemyTuning enemies = new EnemyTuning();
        public BossTuning boss = new BossTuning();

        [Header("Camera")]
        public float cameraSize = 9f;
        public float lookAhead = 1.4f;
        public float cameraSmoothing = 7f;

        enum Flow { Title, Playing, Paused, Victory }

        World world;
        Transform worldRoot;
        LevelView levelView;
        PlayerView playerView;
        readonly List<CrabView> crabs = new List<CrabView>();
        readonly List<JellyView> jellies = new List<JellyView>();
        readonly List<PumpMachineView> machines = new List<PumpMachineView>();
        readonly List<ArchiveFoeView> archiveFoes = new List<ArchiveFoeView>();
        BossView bossView;
        Fx fx;
        AudioSynth audioSynth;
        readonly GameInput input = new GameInput();
        Camera cam;
        RenderTexture pixelFrame;
        Texture2D belfry;
        const int PixelWidth = 512, PixelHeight = 288;
        Flow flow = Flow.Title;
        float acc, hitStop, shake, fade, bannerTime, stepClock, lookX;
        float shakeTime, shakeStrength;
        bool reducedFlashing;
        bool automatedTour;
        string banner = "", bannerSub = "";
        bool debug;
        bool[] roomSeen;
        Vector3 camPos;
        float fps = 60f;
        Dummy testTarget;
        MovementMetrics testMetrics;
        Vec2 previousFeet, takeoff, dashStart;
        bool measuringJump, measuringDash;
        float jumpRise;
        string lastJump = "", lastDash = "";

        void Awake()
        {
            belfry = Resources.Load<Texture2D>("Belfry");
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.04f, 0.06f);
            cam.nearClipPlane = -50f;
            cam.farClipPlane = 50f;
            pixelFrame = new RenderTexture(PixelWidth, PixelHeight, 16)
            {
                name = "Drowned Bell 512x288",
                filterMode = FilterMode.Point,
                antiAliasing = 1,
                useMipMap = false,
            };
            cam.targetTexture = pixelFrame;
            cam.aspect = PixelWidth / (float)PixelHeight;
            var displayCamera = new GameObject("Pixel display camera").AddComponent<Camera>();
            displayCamera.transform.SetParent(transform, false);
            displayCamera.clearFlags = CameraClearFlags.SolidColor;
            displayCamera.backgroundColor = Palette.SkyBottom;
            displayCamera.cullingMask = 0;
            displayCamera.depth = cam.depth + 1f;
            audioSynth = new AudioSynth(gameObject);
            shakeStrength = PlayerPrefs.GetFloat("ShakeStrength", 1f);
            reducedFlashing = PlayerPrefs.GetInt("ReducedFlashing", 0) != 0;
            // In a browser, -1 follows the display's refresh; a fixed rate there would drop requestAnimationFrame.
            Application.targetFrameRate = GameInput.Web ? -1 : 120;
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-testroom") >= 0)
                BeginTestRoom();
            else
                NewRun();
            AutoTour.AttachIfRequested(this);
            if (testTarget == null) AutoPlay.AttachIfRequested(this);
            save = SaveStore.Load();
        }

        // ---- save and continue -----------------------------------------------------------------

        /// <summary>The save on disk, shown on the title as Continue. Null when there is none.</summary>
        SaveData save;

        void Save()
        {
            if (automatedTour || pilot != null || testTarget != null) return;
            save = world.Snapshot();
            SaveStore.Write(save);
        }

        void ContinueSaved()
        {
            NewRun(continuation: World.Restore(save, movement, combat, enemies, boss));
            BeginDive();
        }

        void StartNewGame()
        {
            SaveStore.Clear();
            save = null;
            NewRun();
            BeginDive();
        }

        void OnDestroy()
        {
            if (pixelFrame == null) return;
            if (cam != null) cam.targetTexture = null;
            pixelFrame.Release();
            Destroy(pixelFrame);
        }

        // ---- hooks for the -autoshot verification tour ----------------------------------------
        public World CurrentWorld => world;
        public void BeginForTour() { automatedTour = true; flow = Flow.Playing; audioSynth.SetMuted(true); input.Clear(); }
        public void BeginAutoplay()
        {
            automatedTour = true;
            BeginDive();
        }

        void BeginDive()
        {
            flow = Flow.Playing;
            audioSynth.Start();
            input.Clear();
        }

        public void TourSwing() { world.Player.Facing = 1; input.PressAttack(); }
        public void TourDebug(bool on) { debug = on; }
        public void TourPause(bool on) { flow = on ? Flow.Paused : Flow.Playing; }
        void BeginTestRoom()
        {
            NewRun(testRoom: true);
            flow = Flow.Playing;
            audioSynth.Start();
        }
        /// <summary>QA autoplay: take each step's input from the autopilot instead of the keyboard.</summary>
        public void SetPilot(System.Func<PlayerInput> p) { pilot = p; }
        System.Func<PlayerInput> pilot;

        void NewRun(bool testRoom = false, World continuation = null)
        {
            pilot = null;
            if (continuation == null) audioSynth.ResetRun();
            if (worldRoot != null) Destroy(worldRoot.gameObject);
            worldRoot = new GameObject("World").transform;
            world = continuation ?? new World(movement, combat, enemies, boss, testRoom ? TestRoom.Build() : null);
            world.Event += OnEvent;
            levelView = new LevelView(world, worldRoot);
            crabs.Clear();
            machines.Clear();
            archiveFoes.Clear();
            jellies.Clear();
            testTarget = null;
            measuringJump = measuringDash = false;
            lastJump = lastDash = "";
            foreach (Enemy e in world.Foes)
            {
                if (e is PumpMachine m) machines.Add(new PumpMachineView(m, worldRoot));
                else if (e is InkMoth || e is TuningWidow || e is Archivist) archiveFoes.Add(new ArchiveFoeView(e, worldRoot));
                else if (e is Crab c) crabs.Add(new CrabView(c, worldRoot));
                else if (e is Jelly j) jellies.Add(new JellyView(j, enemies, worldRoot));
                else if (e is Dummy target) testTarget = target;
            }
            bossView = !world.Level.HasBoss ? null : new BossView(world.Boss, boss, worldRoot, world.Level.FloorBelow(world.Level.BossSpawn.X, world.Level.BossSpawn.Y + 0.5f));
            if (testRoom) testMetrics = Metrics.Measure(movement, combat);
            playerView = new PlayerView(world, worldRoot);
            fx = new Fx(worldRoot);
            roomSeen = new bool[world.Level.Rooms.Count];
            acc = hitStop = shake = fade = 0f;
            input.Clear();
            camPos = Target(true);
            banner = bannerSub = "";
            if (!testRoom) ShowRoomBanner(world.Room);
        }

        // ---- events -> presentation -----------------------------------------------------------

        void OnEvent(GameEvent e)
        {
            switch (e.Type)
            {
                // Save points: lamps and lifts, pickups, opened shortcuts, boss clears, and every respawn.
                case GameEventType.LampLit: case GameEventType.Rested: case GameEventType.LungTaken: case GameEventType.WinchPulled:
                case GameEventType.GaugeFound: case GameEventType.LiftOnline: case GameEventType.BypassOnline: case GameEventType.PumpKeyTaken:
                case GameEventType.BossDefeated: case GameEventType.Respawned:
                case GameEventType.LedgerFound: case GameEventType.DrumKeyTaken: case GameEventType.OrderTaken: case GameEventType.ArchivistDefeated:
                    Save();
                    break;
                case GameEventType.EnemyKilled:
                    if (world.Warden != null && !world.Warden.Alive) Save();
                    break;
                case GameEventType.Victory:
                    // The end of the current build: the next dive starts fresh, so there is nothing to continue.
                    // Earlier chapters save the start of the next one (World.Snapshot handles that).
                    if (world.Chapter == Chapter.Archive && !automatedTour) { SaveStore.Clear(); save = null; }
                    else Save();
                    break;
            }
            if (testTarget != null) TrackTestMove(e.Type);
            audioSynth.OnEvent(e);
            Vector2 p = new Vector2(e.Pos.X, e.Pos.Y);
            switch (e.Type)
            {
                case GameEventType.Jump: playerView.OnJump(); fx.Burst(p, Palette.A(Palette.RockEdge, 0.7f), 5, 2f, 0.12f, 6f, 1f); break;
                case GameEventType.Land: playerView.OnLand(); fx.Burst(p, Palette.A(Palette.RockEdge, 0.7f), 6, 2.5f, 0.12f, 6f, 0.5f); break;
                case GameEventType.Dash: fx.Puff(new Vector2(p.x - e.Id * 0.4f, p.y), Palette.A(Palette.VisorGlow, 0.3f), 1.6f, 0.3f); break;
                case GameEventType.Swing: playerView.OnSwing(world.Player.Attack, world.Player.Facing); break;
                case GameEventType.Pogo: fx.Burst(p, Color.white, 8, 4f, 0.1f, 4f); break;
                case GameEventType.HitEnemy: Impact(p, Palette.Suit, 0.32f, 8); break;
                case GameEventType.EnemyKilled: fx.Ring(p, Palette.JellyRing, 1.6f, 0.35f); break;
                case GameEventType.PlayerHurt:
                case GameEventType.HazardHit:
                    playerView.OnHurt(); Impact(p, Palette.Danger, 0.44f, 10); break;
                case GameEventType.PlayerDied: Impact(p, Palette.Suit, 0.72f, 26); break;
                case GameEventType.Respawned: fx.Ring(p, Palette.Warm, 2.5f, 0.6f); camPos = Target(true); break;
                case GameEventType.LampLit:
                    fx.Ring(new Vector2(p.x, p.y + 1.7f), Palette.Warm, 3.5f, 0.9f);
                    fx.Burst(new Vector2(p.x, p.y + 1.7f), Palette.Warm, 18, 3f, 0.1f, -1f);
                    Banner("Tide lamp lit", "You will wake here if the water takes you.");
                    break;
                case GameEventType.Rested: fx.Burst(new Vector2(p.x, p.y + 1.7f), Palette.Warm, 8, 2f, 0.08f, -1f); break;
                case GameEventType.WinchPulled:
                    Impact(p, Palette.Warm, 0.34f, 10);
                    if (world.Chapter == Chapter.Archive) Banner("The counterweight drops", "The chute is open. Up rides between here and the lamp.");
                    else Banner("The winch turns", world.Level.Outlet.HasValue ? "The inspection gate is open. Follow the pipe east." : "Far above, a grate grinds open.");
                    break;
                case GameEventType.BypassOnline: Banner("Intake bypass", "Up rides between the well and intake lamp."); break;
                case GameEventType.GaugeFound: Banner("Keeper record", "The gauge plate bears the night shift's names."); break;
                case GameEventType.LiftOnline: Banner("Counterweight lift", "Checkpoint lit. Up rides the lift to the intake."); break;
                case GameEventType.PumpKeyTaken: Banner("Pump key", "The archive hatch is east of the duty pump."); break;
                case GameEventType.PumpSurge: Banner("Overpressure", "Two pipes vent in sequence."); break;
                case GameEventType.LedgerFound: Banner("Keeper record", "A ledger page: the night shift signed the sea-gate log."); break;
                case GameEventType.DrumKeyTaken: Banner("Drum key", "It fits the choir door in the Return Chute."); break;
                case GameEventType.OrderTaken: Banner("Sea-gate order", "Signed and sealed. The exit east leads to the engine room."); break;
                case GameEventType.ArchivistAwake: AddTrauma(0.44f); Banner("Archivist of Depths", "Only the real one swings its clapper."); break;
                case GameEventType.ArchivistLand: Impact(p, Palette.Wave, 0.5f, 16); break;
                case GameEventType.ArchivistPhase2: AddTrauma(0.55f); fx.Ring(p, Palette.Warm, 6f, 0.8f); break;
                case GameEventType.ArchivistDefeated: Impact(p, Palette.BellRim, 0.9f, 34); fx.Ring(p, Palette.Warm, 10f, 2f); break;
                case GameEventType.MothLodged: fx.Burst(p, Palette.A(Palette.RockEdge, 0.7f), 6, 2.5f, 0.12f, 6f, 0.5f); break;
                case GameEventType.ChuteRide: fx.Ring(p, Palette.Warm, 2f, 0.5f); camPos = Target(true); break;
                case GameEventType.LungTaken:
                    fx.Ring(p, Palette.Warm, 2.5f, 0.6f);
                    Banner("Brass Lung", "One more breath. Your health grows by one mask.");
                    break;
                case GameEventType.RoomEntered: ShowRoomBanner(e.Id); break;
                case GameEventType.JellyPulse: AddTrauma(0.12f); break;
                case GameEventType.RippleToll: Impact(p, Palette.Wave, 0.13f, 3); break;
                case GameEventType.BossAwake: AddTrauma(0.44f); Banner("The Undertoll", "It still remembers how to ring."); break;
                case GameEventType.BossSlam: Impact(p, Palette.Wave, 0.78f, 26); break;
                case GameEventType.BossWallHit: Impact(p, Palette.RockEdge, 0.72f, 20); break;
                case GameEventType.DripSplash: fx.Burst(p, Palette.Drip, 8, 3f, 0.1f, 8f, 1f); break;
                case GameEventType.BossHit: Impact(p, Palette.BellRim, 0.38f, 10); break;
                case GameEventType.BossPhase2: AddTrauma(0.55f); fx.Ring(p, Palette.Warm, 6f, 0.8f); break;
                case GameEventType.BossDefeated: Impact(p, Palette.BellRim, 0.9f, 34); fx.Ring(p, Palette.Warm, 10f, 2f); break;
                case GameEventType.Victory: flow = Flow.Victory; break;
            }
        }

        void AddTrauma(float amount) => shake = Mathf.Clamp01(shake + amount);

        void Impact(Vector2 pos, Color color, float trauma, int particles)
        {
            AddTrauma(trauma);
            fx.Burst(pos, color, particles, 6f, 0.13f, 4f);
        }

        void TrackTestMove(GameEventType type)
        {
            switch (type)
            {
                case GameEventType.Jump:
                    takeoff = previousFeet;
                    jumpRise = 0f;
                    measuringJump = true;
                    break;
                case GameEventType.Land:
                    if (measuringJump)
                        lastJump = $"Last jump  {jumpRise:0.00} up   {Mathf.Abs(world.Player.Body.Feet.X - takeoff.X):0.00} across";
                    measuringJump = false;
                    break;
                case GameEventType.Dash:
                    dashStart = previousFeet;
                    measuringDash = true;
                    break;
                case GameEventType.PlayerHurt:
                case GameEventType.Respawned:
                    measuringJump = measuringDash = false;
                    break;
            }
        }

        void Banner(string title, string sub)
        {
            banner = title;
            bannerSub = sub;
            bannerTime = 0f;
        }

        void ShowRoomBanner(int room)
        {
            if (room < 0 || roomSeen == null || roomSeen[room]) return;
            roomSeen[room] = true;
            string name = world.Level.Rooms[room].Name;
            Banner(name, world.Level.Outlet.HasValue ? world.Level.Rooms[room].Purpose : name == "Ripple Walk" ? "When the bell rings, jump the floor ripple." : "");
        }

        // ---- loop -----------------------------------------------------------------------------

        /// <summary>Losing focus drops into the pause menu, so play never resumes the instant the window is clicked.</summary>
        void OnApplicationFocus(bool focused)
        {
            if (!focused && flow == Flow.Playing && !automatedTour) flow = Flow.Paused;
        }

        /// <summary>The same code path as pressing Enter on the victory screen.</summary>
        internal void ContinueJourney()
        {
            if (flow != Flow.Victory) return;
            World next = world.Chapter == Chapter.Belfry && world.Level.HasBoss ? world.EnterSluice()
                : world.Chapter == Chapter.Sluice ? world.EnterArchive() : null;
            NewRun(continuation: next);
            if (next != null) Save();
            flow = Flow.Playing;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            fps = Mathf.Lerp(fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-4f), 0.05f);
            input.Poll();
            if (GameInput.Debug) debug = !debug;
            if (GameInput.Mute) audioSynth.SetMuted(!audioSynth.Muted);
            if (GameInput.Quit && flow != Flow.Playing) { Application.Quit(); return; }

            switch (flow)
            {
                case Flow.Title:
                    if (Input.GetKeyDown(KeyCode.T)) BeginTestRoom();
                    else if (save != null && Input.GetKeyDown(KeyCode.N)) StartNewGame();
                    else if (GameInput.ConfirmOrClick) { if (save != null) ContinueSaved(); else BeginDive(); }
                    break;
                case Flow.Paused:
                    if (GameInput.CycleShake)
                    {
                        shakeStrength = shakeStrength <= 0f ? 1f : shakeStrength - 0.5f;
                        PlayerPrefs.SetFloat("ShakeStrength", shakeStrength);
                    }
                    if (GameInput.ToggleFlashing)
                    {
                        reducedFlashing = !reducedFlashing;
                        PlayerPrefs.SetInt("ReducedFlashing", reducedFlashing ? 1 : 0);
                    }
                    if (testTarget != null && Input.GetKeyDown(KeyCode.T)) { NewRun(); flow = Flow.Title; }
                    else if (GameInput.Pause || GameInput.ConfirmOrClick) { flow = Flow.Playing; input.Clear(); }
                    break;
                case Flow.Victory:
                    if (GameInput.ConfirmOrClick) ContinueJourney();
                    break;
                case Flow.Playing:
                    if (GameInput.Pause) { flow = Flow.Paused; break; }
                    Simulate(dt);
                    break;
            }
            audioSynth.Tick(flow == Flow.Playing ? dt : 0f);
            bannerTime += dt;
        }

        void Simulate(float dt)
        {
            if (hitStop > 0f) { hitStop -= dt; return; }
            acc += dt;
            int steps = 0;
            while (acc >= World.FixedDt && steps < 5)
            {
                PlayerInput step = input.Take(cam, new Vector2(world.Player.Body.Pos.X, world.Player.Body.Pos.Y), DisplayRect());
                if (pilot != null) step = pilot();
                previousFeet = world.Player.Body.Feet;
                world.Update(World.FixedDt, step);
                if (measuringJump) jumpRise = Mathf.Max(jumpRise, world.Player.Body.Feet.Y - takeoff.Y);
                if (measuringDash && !world.Player.Dashing)
                {
                    lastDash = $"Last dash  {Mathf.Abs(world.Player.Body.Feet.X - dashStart.X):0.00} across";
                    measuringDash = false;
                }
                acc -= World.FixedDt;
                steps++;
                if (world.HitStop > 0f)
                {
                    hitStop = Mathf.Min(world.HitStop, 0.145f);
                    world.HitStop = 0f;
                    acc = 0f;
                    break;
                }
            }
            if (steps == 5) acc = 0f;

            // Footsteps
            Body b = world.Player.Body;
            if (b.Grounded && Mathf.Abs(b.Vel.X) > 2f)
            {
                stepClock -= dt;
                if (stepClock <= 0f) { stepClock = 0.27f; audioSynth.Footstep(); }
            }
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            bool dying = world.Phase == Phase.Dying;
            levelView.Tick(dt);
            playerView.Tick(dt, dying, reducedFlashing);
            foreach (CrabView c in crabs) c.Tick(dt, reducedFlashing);
            foreach (JellyView j in jellies) j.Tick(dt, reducedFlashing);
            foreach (var machine in machines) machine.Tick(reducedFlashing);
            foreach (var foe in archiveFoes) foe.Tick(dt, reducedFlashing, boss.WaveHeight);
            bossView?.Tick(dt, reducedFlashing);
            fx.Tick(flow == Flow.Paused ? 0f : dt);

            fade = Mathf.MoveTowards(fade, dying ? 1f : 0f, dt * (dying ? 1.3f : 2.5f));
            cam.orthographicSize = cameraSize;
            Vector3 target = Target(false);
            camPos = Vector3.Lerp(camPos, target, 1f - Mathf.Exp(-dt * cameraSmoothing));
            shakeTime += Time.unscaledDeltaTime;
            shake = Mathf.Max(0f, shake - Time.unscaledDeltaTime * 1.8f);
            float amount = shake * shake * shakeStrength * 0.36f;
            var j2 = new Vector2(
                0.6f * Mathf.Sin(shakeTime * 37f) + 0.4f * Mathf.Sin(shakeTime * 69f),
                0.6f * Mathf.Sin(shakeTime * 43f) + 0.4f * Mathf.Sin(shakeTime * 79f)) * amount;
            cam.transform.position = Art.Snap(new Vector3(camPos.x + j2.x, camPos.y + j2.y, -10f));
        }

        /// <summary>Follow the diver with a little look-ahead, clamped so the camera never shows outside the current room.</summary>
        Vector3 Target(bool snap)
        {
            Body b = world.Player.Body;
            if (snap) lookX = 0f;
            lookX = Mathf.Lerp(lookX, world.Player.Facing * lookAhead, snap ? 1f : 0.04f);
            float x = b.Pos.X + lookX, y = b.Pos.Y + 1f;
            int ri = world.Room;
            if (ri >= 0)
            {
                RoomInfo r = world.Level.Rooms[ri];
                float hh = cameraSize, hw = cameraSize * cam.aspect;
                x = r.MaxX - r.MinX <= hw * 2f ? (r.MinX + r.MaxX) * 0.5f : Mathf.Clamp(x, r.MinX + hw, r.MaxX - hw);
                y = r.MaxY - r.MinY <= hh * 2f ? (r.MinY + r.MaxY) * 0.5f : Mathf.Clamp(y, r.MinY + hh, r.MaxY - hh);
            }
            return new Vector3(x, y, -10f);
        }

        // ---- HUD ------------------------------------------------------------------------------

        static Rect DisplayRect()
        {
            float fit = Mathf.Min(Screen.width / (float)PixelWidth, Screen.height / (float)PixelHeight);
            float integer = Mathf.Floor(fit);
            float scale = integer >= 1f && integer >= fit * 0.85f ? integer : fit;
            float w = PixelWidth * scale, h = PixelHeight * scale;
            return new Rect(Mathf.Floor((Screen.width - w) * 0.5f), Mathf.Floor((Screen.height - h) * 0.5f), w, h);
        }

        const int big = 3, mid = 1, small = -1, mono = -1;

        static void Box(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Label(Rect r, string s, int size, Color color) => PixelFont.Draw(r, s, Mathf.Abs(size), size > 0, color);

        void OnGUI()
        {
            if (world == null || Event.current.type != EventType.Repaint) return;
            Rect display = DisplayRect();
            GUI.DrawTexture(display, pixelFrame);
            Matrix4x4 original = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(display.x, display.y), Quaternion.identity,
                new Vector3(display.width / PixelWidth, display.height / PixelHeight, 1f));
            DrawHud();
            GUI.matrix = original;
        }

        void DrawHud()
        {
            float w = PixelWidth, h = PixelHeight, u = h / 100f;

            if (fade > 0.01f) Box(new Rect(0, 0, w, h), new Color(0.01f, 0.02f, 0.03f, fade));

            if (flow != Flow.Title)
            {
                // Masks
                Texture mask = PixelArt.Mask.texture;
                for (int i = 0; i < world.Player.MaxHealth; i++)
                {
                    var r = new Rect(3 * u + i * 5.2f * u, 3 * u, 4.2f * u, 4.2f * u);
                    Color old = GUI.color;
                    GUI.color = new Color(0, 0, 0, 0.5f);
                    GUI.DrawTexture(new Rect(r.x - 0.3f * u, r.y - 0.3f * u, r.width + 0.6f * u, r.height + 0.6f * u), mask);
                    GUI.color = i < world.Player.Health ? Palette.Suit : new Color(0.25f, 0.33f, 0.37f, 0.9f);
                    GUI.DrawTexture(r, mask);
                    GUI.color = old;
                }
                if (world.LampLit) Label(new Rect(3 * u, 8.5f * u, 60 * u, 4 * u), "lamp lit" + (world.GrateOpen ? (world.Chapter == Chapter.Archive ? "   chute open" : "   grate open") : ""), small, Palette.A(Palette.Warm, 0.8f));
                if (testTarget != null)
                {
                    Label(new Rect(3 * u, 9 * u, 56 * u, 4 * u), $"Test Room   Target hits {testTarget.Hits}", small, Palette.Warm);
                    Label(new Rect(3 * u, 13 * u, 98 * u, 16 * u),
                        $"Jump {testMetrics.FullJumpHeight:0.00} up / {testMetrics.RunningJumpReach:0.00} across\n" +
                        $"Dash {testMetrics.DashReach:0.00}   Jump + dash {testMetrics.DashJumpReach:0.00}\n{lastJump}\n{lastDash}",
                        small, Palette.Text);
                }

                // Boss bar
                Boss bs = world.Boss;
                PumpMachine pump = world.Warden;
                Archivist ink = world.Archivist;
                bool pumping = pump != null && pump.Engaged && pump.Alive && pump.Room == world.Room;
                bool inking = ink != null && ink.Engaged && ink.Alive && ink.Room == world.Room;
                if (pumping || inking || (bs.Awake && !bs.Dead && world.Room == world.ArenaRoom))
                {
                    float bw = w * 0.4f, bx = (w - bw) * 0.5f, by = 7.5f * u;
                    Label(new Rect(0, by - 4.6f * u, w, 4 * u), inking ? "ARCHIVIST OF DEPTHS" : pumping ? "PUMP WARDEN" : "THE UNDERTOLL", mid, Palette.Text);
                    Box(new Rect(bx - 2, by - 2, bw + 4, 1.6f * u + 4), new Color(0, 0, 0, 0.6f));
                    Box(new Rect(bx, by, bw * (inking ? (float)ink.Health / ink.MaxHealth : pumping ? (float)pump.Health / pump.MaxHealth : (float)bs.Health / bs.MaxHealth), 1.6f * u), Palette.BellRim);
                }

                // Banner: room names and story beats, fading in and out.
                if (bannerTime < 3.2f && banner.Length > 0)
                {
                    float a = Mathf.Clamp01(bannerTime * 3f) * Mathf.Clamp01((3.2f - bannerTime) * 2f);
                    Label(new Rect(0, h * 0.14f, w, 8 * u), banner, big, Palette.A(Palette.Text, a));
                    if (bannerSub.Length > 0) Label(new Rect(0, h * 0.14f + 7.5f * u, w, 4 * u), bannerSub, mid, Palette.A(Palette.TextDim, a));
                }
            }

            switch (flow)
            {
                case Flow.Title:
                    DrawBookend(false);
                    break;
                case Flow.Paused:
                    Overlay("PAUSED", "Esc, Enter or click to resume." + (GameInput.Web ? "" : "   Q to quit."),
                        (testTarget != null ? "T returns to the title.\n" : "Down-swing in the air to bounce off enemies and spikes.\n") +
                        $"1 camera shake {shakeStrength:P0}   2 reduced flashing {(reducedFlashing ? "on" : "off")}");
                    break;
                case Flow.Victory:
                    DrawBookend(true);
                    break;
            }

            if (debug) DrawDebug(w, h, u);
        }

        void DrawBookend(bool victory)
        {
            Box(new Rect(0, 0, PixelWidth, PixelHeight), Palette.SkyBottom);
            GUI.DrawTexture(new Rect(320, 0, 192, 288), belfry);
            Box(new Rect(319, 0, 1, 288), Palette.RockSide);
            bool outlet = world.Chapter != Chapter.Belfry;
            bool final = world.Chapter == Chapter.Archive;
            const int x = 22, width = 280;
            Label(new Rect(x, 35, width, 60), victory ? (final ? "THE ORDER\nIS YOURS" : outlet ? "PUMP\nSILENCED" : "THE BELL IS\nSILENT") : "DROWNED\nBELL", -3, Palette.Warm);
            Label(new Rect(x, 109, width, 34), victory
                ? (final ? "The sea-gate order names who drowned the halls. The engine room lies below. End of the current build."
                    : outlet ? "The pumps fall quiet. The key opens the hatch to the Sounding Archive." : "Below the silent bell, an intake has opened. The pumps are still running.")
                : "A salvage diver descends a flooded tower to silence the bell that keeps drowning the halls.", small, Palette.Text);
            Box(new Rect(x, 151, 32, 1), Palette.WarmDeep);
            string details = victory
                ? "Time   " + FormatTime(world.Time) + "\nDeaths   " + world.Deaths + "\nBrass Lung   " + (world.LungTaken ? "found" : "missed")
                : "A/D move    Space jump    Shift dash\nX/J swing   W/S aim\nLeft click swing    Right click dash\nEsc pause   M mute   " + (GameInput.Web ? "I" : "F1") + " inspector";
            if (victory && outlet) details += "\nKeeper records   " + ((world.GaugeFound ? 1 : 0) + (world.LedgerFound ? 1 : 0)) + " of " + (final ? 2 : 1);
            Label(new Rect(x, 169, width, 48), details, small, Palette.TextDim);
            bool resume = !victory && save != null;
            Label(new Rect(x, 231, width, 12), victory ? (final ? "Enter or click to dive again" : outlet ? "Enter or click to enter the Sounding Archive" : "Enter or click to enter the Sluice Works") : resume ? "Enter or click to continue" : "Enter or click to dive", small, Palette.Warm);
            if (resume) Label(new Rect(x, 242, width, 12), "Saved   " + save.Summary(), small, Palette.Text);
            Label(new Rect(x, resume ? 260 : 252, width, 12), (resume ? "N new game    " : "") + (victory ? "" : "T test room    ") + (GameInput.Web ? "" : "Q quit"), small, Palette.TextDim);
        }

        void Overlay(string title, string sub, string body)
        {
            float w = PixelWidth, h = PixelHeight, u = h / 100f;
            Box(new Rect(0, 0, w, h), new Color(0.01f, 0.03f, 0.05f, 0.78f));
            Label(new Rect(0, h * 0.24f, w, 10 * u), title, big, Palette.Warm);
            Label(new Rect(w * 0.15f, h * 0.36f, w * 0.7f, 6 * u), sub, mid, Palette.Text);
            Label(new Rect(w * 0.12f, h * 0.48f, w * 0.76f, 30 * u), body, mid, Palette.TextDim);
        }

        static string FormatTime(float s) => ((int)s / 60) + ":" + ((int)s % 60).ToString("00");

        // ---- F1 inspector ---------------------------------------------------------------------

        void Outline(Aabb b, Color c)
        {
            Vector3 a = cam.WorldToViewportPoint(new Vector3(b.MinX, b.MinY));
            Vector3 z = cam.WorldToViewportPoint(new Vector3(b.MaxX, b.MaxY));
            var r = new Rect(a.x * PixelWidth, (1f - z.y) * PixelHeight, (z.x - a.x) * PixelWidth, (z.y - a.y) * PixelHeight);
            Box(new Rect(r.x, r.y, r.width, 2), c);
            Box(new Rect(r.x, r.yMax - 2, r.width, 2), c);
            Box(new Rect(r.x, r.y, 2, r.height), c);
            Box(new Rect(r.xMax - 2, r.y, 2, r.height), c);
        }

        void DrawDebug(float w, float h, float u)
        {
            Player p = world.Player;
            Outline(p.Body.Box, Color.cyan);
            if (p.AttackActive(combat)) Outline(p.AttackBox(combat, movement), Color.white);
            foreach (Enemy e in world.Foes)
            {
                if (!e.Alive || e.Room != world.Room) continue;
                Outline(e.Body.Box, e.Harmful ? Color.yellow : Color.green);
                if (e.TryAttackBox(out Aabb box)) Outline(box, Color.red);
            }
            if (world.Room == world.ArenaRoom)
            {
                Outline(world.Boss.Body.Box, world.Boss.Harmful ? Color.yellow : Color.green);
                foreach (Shockwave s in world.Boss.Waves) Outline(Boss.WaveBox(s, boss), Color.red);
                foreach (Drip d in world.Boss.Drips) if (d.Falling) Outline(Boss.DripBox(d), Color.red);
            }

            var lines = new List<string>
            {
                "INSPECTOR (F1 / I)   fps " + fps.ToString("0") + "   sim 60 Hz   time " + FormatTime(world.Time) + "   deaths " + world.Deaths,
                "room " + (world.Room >= 0 ? world.Level.Rooms[world.Room].Name : "-") + "   grate " + (world.GrateOpen ? "open" : "closed") + "   doors " + (world.Level.DoorsLocked ? "locked" : "open"),
                "diver pos " + p.Body.Pos + " vel " + p.Body.Vel + (p.Body.Grounded ? " grounded" : " air") + (p.Body.OnOneWay ? " (ledge)" : "") + (p.Dashing ? " DASH" : "") + (p.AirDashUsed ? " airdash-used" : ""),
                "hp " + p.Health + "/" + p.MaxHealth + "   invuln " + p.Invuln.ToString("0.00") + "   stun " + p.Stun.ToString("0.00") + "   swing " + (p.Attacking ? p.Attack + " " + p.AttackTime.ToString("0.00") : "-"),
            };
            foreach (Enemy e in world.Foes)
                if (e.Room == world.Room) lines.Add(e.Kind.ToString() + " " + e.Id + "  " + (e.Alive ? e.StateName + "  hp " + e.Health : "dead") + "  rejected " + e.RejectedTransitions);
            if (world.Room == world.ArenaRoom)
                lines.Add("undertoll " + world.Boss.State + "  hp " + world.Boss.Health + "/" + world.Boss.MaxHealth + "  phase " + (world.Boss.Phase2 ? 2 : 1) + "  attacks " + world.Boss.AttackCount + "  waves " + world.Boss.Waves.Count + "  drips " + world.Boss.Drips.Count);
            lines.Add("cyan diver, white swing, yellow harmful body, green safe body, red live attack");
            lines.Add("Tuning: select the 'Drowned Bell' object and edit it in the Inspector.");
            const float lineHeight = 18f;
            Box(new Rect(u, h - lines.Count * lineHeight - 2 * u, w - 2 * u, lines.Count * lineHeight + u), new Color(0, 0, 0, 0.7f));
            for (int i = 0; i < lines.Count; i++)
                Label(new Rect(1.6f * u, h - (lines.Count - i) * lineHeight - 1.6f * u, w - 4 * u, lineHeight), lines[i], mono, Color.white);
        }
    }
}
