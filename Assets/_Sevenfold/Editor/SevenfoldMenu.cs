using System.Collections.Generic;
using System.IO;
using Sevenfold.Board;
using Sevenfold.Core;
using Sevenfold.Dice;
using Sevenfold.Player;
using Sevenfold.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sevenfold.EditorTools
{
    public static class SevenfoldMenu
    {
        private const string RootDataPath = "Assets/_Sevenfold/Data";
        private const string ConfigPath = "Assets/_Sevenfold/Data/Config/GameConfig.asset";
        private const string BoardPath = "Assets/_Sevenfold/Data/Boards/Board_Main.asset";
        private const string PlayerPresentationPath = "Assets/_Sevenfold/Data/Config/PlayerPresentation.asset";
        private const string InputConfigPath = "Assets/_Sevenfold/Data/Config/InputConfig.asset";

        private const string TileMaterialPath = "Assets/_Sevenfold/Art/Materials/M_Tile_Default.mat";
        private const string PlayerBodyMaterialPath = "Assets/_Sevenfold/Art/Materials/M_Player_Body.mat";
        private const string PlayerHatMaterialPath = "Assets/_Sevenfold/Art/Materials/M_Player_Hat.mat";
        private const string DiceIvoryMaterialPath = "Assets/_Sevenfold/Art/Materials/M_Dice_Ivory.mat";
        private const string DicePipMaterialPath = "Assets/_Sevenfold/Art/Materials/M_Dice_Pip.mat";
        private const string ConeHatMeshPath = "Assets/_Sevenfold/Art/Models/PlayerHat_Cone.asset";
        private const string PlayerPrefabPath = "Assets/_Sevenfold/Prefabs/Player/PlayerPawn.prefab";

        private const string TileNormalPath = "Assets/_Sevenfold/Data/Tiles/Tile_Normal.asset";
        private const string TileStarPath = "Assets/_Sevenfold/Data/Tiles/Tile_Star.asset";
        private const string TileTrapPath = "Assets/_Sevenfold/Data/Tiles/Tile_Trap.asset";
        private const string TileCharmPath = "Assets/_Sevenfold/Data/Tiles/Tile_Charm.asset";
        private const string TileGapPath = "Assets/_Sevenfold/Data/Tiles/Tile_Gap.asset";
        private const string TileGatePath = "Assets/_Sevenfold/Data/Tiles/Tile_Gate.asset";
        private const string TileStartPath = "Assets/_Sevenfold/Data/Tiles/Tile_Start.asset";

        private const string BlessingFatesSightPath = "Assets/_Sevenfold/Data/Blessings/Blessing_FatesSight.asset";
        private const string BlessingFortuneWardPath = "Assets/_Sevenfold/Data/Blessings/Blessing_FortuneWard.asset";
        private const string BlessingSecondChancePath = "Assets/_Sevenfold/Data/Blessings/Blessing_SecondChance.asset";

        private const string GameplayScenePath = "Assets/_Sevenfold/Scenes/Gameplay.unity";
        private const string MainMenuScenePath = "Assets/_Sevenfold/Scenes/MainMenu.unity";

        [MenuItem("Sevenfold/Setup/Create Or Update Default Data")]
        public static void CreateOrUpdateDefaultData()
        {
            EnsureDataDirectories();

            // 1. Tiles
            var tileNormal = SetupTile(TileNormalPath, "tile_normal", "Normal Tile", TileKind.Normal,
                "A standard numbered tile.", "#A8D5D1", 0, false, false, "step_normal");

            var tileStar = SetupTile(TileStarPath, "tile_star", "Fortune Star", TileKind.Star,
                "A bright star. Collect 7 to open the Fortune Gate.", "#F2C14E", 1, true, false, "pickup_star");

            var tileTrap = SetupTile(TileTrapPath, "tile_trap", "Hazard Trap", TileKind.Trap,
                "Damages for 1 heart unless shielded by a Ward charge.", "#7B2D5B", 1, true, true, "trigger_trap");

            var tileCharm = SetupTile(TileCharmPath, "tile_charm", "Fortune Charm", TileKind.Charm,
                "Grants 1 Charm charge to activate Ward protection.", "#2E9E6B", 1, true, true, "pickup_charm");

            var tileGap = SetupTile(TileGapPath, "tile_gap", "Chasm Gap", TileKind.Gap,
                "An impassable void.", "#1B1B2A", 0, false, false, "");

            var tileGate = SetupTile(TileGatePath, "tile_gate", "Fortune Gate", TileKind.Gate,
                "The path of victory. Opens when 7 stars are gathered.", "#6A4C93", 0, false, false, "gate_victory");

            var tileStart = SetupTile(TileStartPath, "tile_start", "Start Point", TileKind.Start,
                "The starting cell of the fortune path.", "#F4EBD0", 0, false, false, "");

            // 2. Game Config
            var config = GetOrCreateScriptableObject<GameConfigSO>(ConfigPath);
            config.BoardWidth = 7;
            config.BoardHeight = 7;
            config.CellSpacing = 1.25f;
            config.TileTopSize = 1.12f;
            config.DiceCount = 2;
            config.DiceSides = 6;
            config.LuckyNumber = 7;
            config.StarsRequired = 7;
            config.StartingHearts = 3;
            config.StartingCharms = 1;
            config.MaxCharms = 3;
            config.MaxTurns = 7;
            config.AutoEndTurn = true;
            config.AutoEndTurnDelay = 0.3f;
            config.PlayerMoveDuration = 0.22f;
            config.PlayerHopHeight = 0.12f;
            config.DiceAnimDuration = 0.7f;
            config.FatesSightRadius = 3;
            config.MusicVolume = 0.35f;
            config.SfxVolume = 0.75f;
            config.CameraShakeEnabled = true;
            config.UseFixedSeed = false;
            config.FixedSeed = 0;
            EditorUtility.SetDirty(config);

            // 3. Board Definition
            var board = GetOrCreateScriptableObject<BoardDefinitionSO>(BoardPath);
            board.Width = 7;
            board.Height = 7;
            board.CellSpacing = 1.25f;
            board.TileTopSize = 1.12f;
            board.StartCell = new Vector2Int(0, 0);
            board.GateCell = new Vector2Int(6, 6);
            board.DefaultNormalTile = tileNormal;

            board.Cells.Clear();

            // Start & Gate
            board.Cells.Add(new CellEntry(new Vector2Int(0, 0), tileStart));
            board.Cells.Add(new CellEntry(new Vector2Int(6, 6), tileGate));

            // Stars (7): (1,6) (5,6) (3,5) (0,4) (1,2) (3,2) (2,0)
            Vector2Int[] stars = {
                new Vector2Int(1, 6),
                new Vector2Int(5, 6),
                new Vector2Int(3, 5),
                new Vector2Int(0, 4),
                new Vector2Int(1, 2),
                new Vector2Int(3, 2),
                new Vector2Int(2, 0)
            };
            foreach (var star in stars)
            {
                board.Cells.Add(new CellEntry(star, tileStar));
            }

            // Traps (4): (4,5) (2,4) (1,1) (4,1)
            Vector2Int[] traps = {
                new Vector2Int(4, 5),
                new Vector2Int(2, 4),
                new Vector2Int(1, 1),
                new Vector2Int(4, 1)
            };
            foreach (var trap in traps)
            {
                board.Cells.Add(new CellEntry(trap, tileTrap));
            }

            // Charms (2): (1,5) (6,1)
            Vector2Int[] charms = {
                new Vector2Int(1, 5),
                new Vector2Int(6, 1)
            };
            foreach (var charm in charms)
            {
                board.Cells.Add(new CellEntry(charm, tileCharm));
            }

            // Gaps (4): (2,5) (4,4) (5,3) (3,1)
            Vector2Int[] gaps = {
                new Vector2Int(2, 5),
                new Vector2Int(4, 4),
                new Vector2Int(5, 3),
                new Vector2Int(3, 1)
            };
            foreach (var gap in gaps)
            {
                board.Cells.Add(new CellEntry(gap, tileGap));
            }

            EditorUtility.SetDirty(board);

            // 4. Blessings
            SetupBlessing(BlessingFatesSightPath, BlessingKind.FatesSight, "Fate's Sight",
                "Reveal radius 3 around player (permanent). Movement budget = 7.", 3, 0, 0);

            SetupBlessing(BlessingFortuneWardPath, BlessingKind.FortuneWard, "Fortune Ward",
                "WardCharges +1 (max 2). Movement budget = 7.", 0, 1, 0);

            SetupBlessing(BlessingSecondChancePath, BlessingKind.SecondChance, "Second Chance",
                "Roll both dice again ONCE. Show both totals; player picks original 7 or the new total. The reroll can never trigger Lucky Fate.", 0, 0, 1);

            // 5. Default Material
            GetOrCreateTileMaterial();

            // 6. Player Presentation Config
            var playerPres = GetOrCreateScriptableObject<PlayerPresentationSO>(PlayerPresentationPath);
            playerPres.MoveDuration = 0.22f;
            playerPres.HopHeight = 0.12f;
            playerPres.LandingSquash = 0.1f;
            playerPres.LandingDuration = 0.1f;
            playerPres.IdleBobAmount = 0.03f;
            playerPres.IdleBobSpeed = 1.5f;
            EditorUtility.SetDirty(playerPres);

            // 7. Input Config
            var inputConfig = GetOrCreateScriptableObject<InputConfigSO>(InputConfigPath);
            inputConfig.RollBindings = new[] { "<Keyboard>/space" };
            inputConfig.UpBindings = new[] { "<Keyboard>/w", "<Keyboard>/upArrow" };
            inputConfig.DownBindings = new[] { "<Keyboard>/s", "<Keyboard>/downArrow" };
            inputConfig.LeftBindings = new[] { "<Keyboard>/a", "<Keyboard>/leftArrow" };
            inputConfig.RightBindings = new[] { "<Keyboard>/d", "<Keyboard>/rightArrow" };
            inputConfig.EndTurnBindings = new[] { "<Keyboard>/enter", "<Keyboard>/numpadEnter" };
            inputConfig.UseCharmBindings = new[] { "<Keyboard>/c" };
            inputConfig.PauseBindings = new[] { "<Keyboard>/escape" };
            EditorUtility.SetDirty(inputConfig);

            // 8. Player Materials & Models
            var coneMesh = GetOrCreateConeMesh();
            var bodyMat = GetOrCreateMaterial(PlayerBodyMaterialPath, "M_Player_Body", "#EE6055");
            var hatMat = GetOrCreateMaterial(PlayerHatMaterialPath, "M_Player_Hat", "#5B2A86");

            // 9. Player Prefab
            CreateOrUpdatePlayerPrefab(playerPres, bodyMat, hatMat, coneMesh);

            // 10. Dice Materials
            GetOrCreateMaterial(DiceIvoryMaterialPath, "M_Dice_Ivory", "#FDF6E2");
            GetOrCreateMaterial(DicePipMaterialPath, "M_Dice_Pip", "#1E1E24");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Sevenfold] Default data assets created/updated successfully.");
        }

        [MenuItem("Sevenfold/Setup/Build Gameplay Scene")]
        public static void BuildGameplayScene()
        {
            EnsureDataDirectories();
            EnsureFolder("Assets/_Sevenfold", "Scenes");

            Scene scene;
            bool sceneFileExists = File.Exists(GameplayScenePath);

            if (sceneFileExists)
            {
                scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, GameplayScenePath);
            }

            // 1. Delete and recreate ONLY roots named __Generated_*
            var rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                var root = rootObjects[i];
                if (root != null && root.name.StartsWith("__Generated_"))
                {
                    Undo.DestroyObjectImmediate(root);
                }
            }

            // 2. Create EnvironmentRoot ONLY if it does not exist (never touch it otherwise)
            bool hasEnvironmentRoot = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root != null && root.name == "EnvironmentRoot")
                {
                    hasEnvironmentRoot = true;
                    break;
                }
            }
            if (!hasEnvironmentRoot)
            {
                var envRoot = new GameObject("EnvironmentRoot");
                Undo.RegisterCreatedObjectUndo(envRoot, "Create EnvironmentRoot");
            }

            // Load data assets
            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(ConfigPath);
            var boardDef = AssetDatabase.LoadAssetAtPath<BoardDefinitionSO>(BoardPath);
            var playerPres = AssetDatabase.LoadAssetAtPath<PlayerPresentationSO>(PlayerPresentationPath);
            var inputConfig = AssetDatabase.LoadAssetAtPath<InputConfigSO>(InputConfigPath);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            if (config == null || boardDef == null || playerPres == null || inputConfig == null || playerPrefab == null)
            {
                CreateOrUpdateDefaultData();
                config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(ConfigPath);
                boardDef = AssetDatabase.LoadAssetAtPath<BoardDefinitionSO>(BoardPath);
                playerPres = AssetDatabase.LoadAssetAtPath<PlayerPresentationSO>(PlayerPresentationPath);
                inputConfig = AssetDatabase.LoadAssetAtPath<InputConfigSO>(InputConfigPath);
                playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            }
            var tileMaterial = GetOrCreateTileMaterial();
            var diceIvoryMat = GetOrCreateMaterial(DiceIvoryMaterialPath, "M_Dice_Ivory", "#FDF6E2");
            var dicePipMat = GetOrCreateMaterial(DicePipMaterialPath, "M_Dice_Pip", "#1E1E24");

            // 3. __Generated_BoardRoot
            var boardRoot = new GameObject("__Generated_BoardRoot");
            Undo.RegisterCreatedObjectUndo(boardRoot, "Create __Generated_BoardRoot");

            var cellsRoot = new GameObject("CellsRoot");
            cellsRoot.transform.SetParent(boardRoot.transform, false);

            var boardBuilder = boardRoot.AddComponent<BoardBuilder>();
            var boardSo = new SerializedObject(boardBuilder);
            boardSo.FindProperty("_board").objectReferenceValue = boardDef;
            boardSo.FindProperty("_cellsRoot").objectReferenceValue = cellsRoot.transform;
            boardSo.FindProperty("_tileMaterial").objectReferenceValue = tileMaterial;
            boardSo.ApplyModifiedPropertiesWithoutUndo();

            // Populate cells in editor scene
            boardBuilder.Build();

            // 4. __Generated_PlayerRoot
            GameObject playerGo;
            if (playerPrefab != null)
            {
                playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                Undo.RegisterCreatedObjectUndo(playerGo, "Create __Generated_PlayerRoot");
            }
            else
            {
                var bodyMat = GetOrCreateMaterial(PlayerBodyMaterialPath, "M_Player_Body", "#EE6055");
                var hatMat = GetOrCreateMaterial(PlayerHatMaterialPath, "M_Player_Hat", "#5B2A86");
                var coneMesh = GetOrCreateConeMesh();
                playerGo = CreatePlayerPawnHierarchy(null, playerPres, bodyMat, hatMat, coneMesh);
                Undo.RegisterCreatedObjectUndo(playerGo, "Create __Generated_PlayerRoot");
            }
            playerGo.name = "__Generated_PlayerRoot";

            var grid = new BoardGrid(boardDef);
            playerGo.transform.position = grid.CellToLocal(boardDef.StartCell);

            var playerMover = playerGo.GetComponent<PlayerGridMover>();
            var moverSo = new SerializedObject(playerMover);
            moverSo.FindProperty("_presentation").objectReferenceValue = playerPres;
            moverSo.FindProperty("_visualChild").objectReferenceValue = playerGo.transform.Find("Visual");
            moverSo.ApplyModifiedPropertiesWithoutUndo();

            // 5. __Generated_DiceRoot (Two 3D Dice beside the board)
            var diceRoot = new GameObject("__Generated_DiceRoot");
            Undo.RegisterCreatedObjectUndo(diceRoot, "Create __Generated_DiceRoot");

            var dieViewA = CreateDiceObject("Die_1", diceRoot.transform, new Vector3(4.8f, 0.3f, -2.2f), diceIvoryMat, dicePipMat, new Vector3(1.3f, 2.7f, 0.9f));
            var dieViewB = CreateDiceObject("Die_2", diceRoot.transform, new Vector3(4.8f, 0.3f, -3.4f), diceIvoryMat, dicePipMat, new Vector3(-1.1f, 2.5f, 1.4f));

            // 6. __Generated_GameRoot
            var gameRoot = new GameObject("__Generated_GameRoot");
            Undo.RegisterCreatedObjectUndo(gameRoot, "Create __Generated_GameRoot");

            var flowController = gameRoot.AddComponent<GameFlowController>();

            var inputRouter = gameRoot.AddComponent<PlayerInputRouter>();
            var inputSo = new SerializedObject(inputRouter);
            inputSo.FindProperty("_config").objectReferenceValue = inputConfig;
            inputSo.FindProperty("_inputEnabled").boolValue = true;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            var turnController = gameRoot.AddComponent<TurnController>();
            var turnSo = new SerializedObject(turnController);
            turnSo.FindProperty("_config").objectReferenceValue = config;
            turnSo.FindProperty("_board").objectReferenceValue = boardDef;
            turnSo.FindProperty("_boardBuilder").objectReferenceValue = boardBuilder;
            turnSo.FindProperty("_playerMover").objectReferenceValue = playerMover;
            turnSo.FindProperty("_flowController").objectReferenceValue = flowController;
            turnSo.FindProperty("_diceViewA").objectReferenceValue = dieViewA;
            turnSo.FindProperty("_diceViewB").objectReferenceValue = dieViewB;
            turnSo.FindProperty("_inputRouter").objectReferenceValue = inputRouter;
            turnSo.ApplyModifiedPropertiesWithoutUndo();

            // 7. __Generated_UIRoot (Canvas + DebugHUD + EventSystem)
            var uiRoot = new GameObject("__Generated_UIRoot");
            Undo.RegisterCreatedObjectUndo(uiRoot, "Create __Generated_UIRoot");
            CreateDebugHUD(uiRoot.transform, flowController, turnController);

            // 8. __Generated_CameraRig
            var camRig = new GameObject("__Generated_CameraRig");
            Undo.RegisterCreatedObjectUndo(camRig, "Create __Generated_CameraRig");
            camRig.tag = "MainCamera";

            var camRot = Quaternion.Euler(40f, 45f, 0f);
            Vector3 camForward = camRot * Vector3.forward;
            // Frame distance ~25 units looking at origin
            camRig.transform.position = -camForward * 25f;
            camRig.transform.rotation = camRot;

            var cam = camRig.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.3f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Pastel sky background #BFD7EA
            cam.backgroundColor = new Color(191f / 255f, 215f / 255f, 234f / 255f, 1f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;

            camRig.AddComponent<AudioListener>();

            var framer = camRig.AddComponent<CameraFramer>();
            var camSo = new SerializedObject(framer);
            camSo.FindProperty("_camera").objectReferenceValue = cam;
            camSo.FindProperty("_board").objectReferenceValue = boardDef;
            camSo.FindProperty("_hudMarginFactor").floatValue = 0.70f;
            camSo.ApplyModifiedPropertiesWithoutUndo();
            framer.RecalculateFraming();

            // 9. __Generated_Lighting
            var lightGo = new GameObject("__Generated_Lighting");
            Undo.RegisterCreatedObjectUndo(lightGo, "Create __Generated_Lighting");
            lightGo.transform.position = new Vector3(0f, 10f, 0f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.96f, 0.90f); // warm white
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;

            // 10. Save scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameplayScenePath);

            // 11. EditorBuildSettings: MainMenu (if exists) at index 0, Gameplay at index 1
            UpdateEditorBuildSettings();

            Debug.Log($"<color=green>[Sevenfold] Gameplay Scene built successfully at {GameplayScenePath}.</color>");
        }

        [MenuItem("Sevenfold/Setup/Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            Debug.Log("[Sevenfold] Build Main Menu Scene: not implemented yet");
        }

        [MenuItem("Sevenfold/Validate/Validate All Data")]
        public static void ValidateAllData()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(ConfigPath);
            var board = AssetDatabase.LoadAssetAtPath<BoardDefinitionSO>(BoardPath);
            var playerPres = AssetDatabase.LoadAssetAtPath<PlayerPresentationSO>(PlayerPresentationPath);
            var inputConfig = AssetDatabase.LoadAssetAtPath<InputConfigSO>(InputConfigPath);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            int failures = 0;

            // Config checks
            if (config == null)
            {
                Debug.LogError($"[Sevenfold] FAIL: GameConfigSO not found at {ConfigPath}");
                failures++;
            }
            else
            {
                if (config.DiceCount >= 1)
                    Debug.Log($"[Sevenfold] PASS: Dice count valid ({config.DiceCount} >= 1)");
                else
                {
                    Debug.LogError($"[Sevenfold] FAIL: Dice count must be >= 1, was {config.DiceCount}");
                    failures++;
                }

                if (config.DiceSides >= 2)
                    Debug.Log($"[Sevenfold] PASS: Dice sides valid ({config.DiceSides} >= 2)");
                else
                {
                    Debug.LogError($"[Sevenfold] FAIL: Dice sides must be >= 2, was {config.DiceSides}");
                    failures++;
                }

                if (config.LuckyNumber > 0)
                    Debug.Log($"[Sevenfold] PASS: Lucky number valid ({config.LuckyNumber} > 0)");
                else
                {
                    Debug.LogError($"[Sevenfold] FAIL: Lucky number must be > 0, was {config.LuckyNumber}");
                    failures++;
                }

                if (config.StartingHearts > 0)
                    Debug.Log($"[Sevenfold] PASS: Starting hearts valid ({config.StartingHearts} > 0)");
                else
                {
                    Debug.LogError($"[Sevenfold] FAIL: Starting hearts must be > 0, was {config.StartingHearts}");
                    failures++;
                }

                if (config.StartingCharms >= 0 && config.MaxCharms >= config.StartingCharms)
                    Debug.Log($"[Sevenfold] PASS: Charm settings valid ({config.StartingCharms} <= {config.MaxCharms})");
                else
                {
                    Debug.LogError($"[Sevenfold] FAIL: Starting charms must be >= 0 and <= MaxCharms.");
                    failures++;
                }

                if (config.StarsRequired > 0)
                    Debug.Log($"[Sevenfold] PASS: Stars required valid ({config.StarsRequired} > 0)");
                else
                {
                    Debug.LogError($"[Sevenfold] FAIL: Stars required must be > 0, was {config.StarsRequired}");
                    failures++;
                }

                if (config.MaxTurns > 0)
                    Debug.Log($"[Sevenfold] PASS: Max turns valid ({config.MaxTurns} > 0)");
                else
                {
                    Debug.LogError($"[Sevenfold] FAIL: Max turns must be > 0, was {config.MaxTurns}");
                    failures++;
                }
            }

            // Board checks
            if (board == null)
            {
                Debug.LogError($"[Sevenfold] FAIL: BoardDefinitionSO not found at {BoardPath}");
                failures++;
            }
            else
            {
                var errors = BoardValidator.Validate(board, config);
                if (errors.Count == 0)
                {
                    Debug.Log($"[Sevenfold] PASS: Board layout ({board.name}) passed all validation checks.");
                }
                else
                {
                    foreach (var error in errors)
                    {
                        Debug.LogError($"[Sevenfold] FAIL: {error}");
                        failures++;
                    }
                }
            }

            // Player presentation checks
            if (playerPres == null)
            {
                Debug.LogError($"[Sevenfold] FAIL: PlayerPresentationSO not found at {PlayerPresentationPath}");
                failures++;
            }
            else
            {
                if (playerPres.MoveDuration > 0f)
                    Debug.Log($"[Sevenfold] PASS: Player move duration valid ({playerPres.MoveDuration}s > 0)");
                else
                {
                    Debug.LogError($"[Sevenfold] FAIL: Player move duration must be > 0, was {playerPres.MoveDuration}");
                    failures++;
                }

                if (playerPres.LandingDuration >= 0f)
                    Debug.Log($"[Sevenfold] PASS: Player landing duration valid ({playerPres.LandingDuration}s >= 0)");
                else
                {
                    Debug.LogError($"[Sevenfold] FAIL: Player landing duration must be >= 0, was {playerPres.LandingDuration}");
                    failures++;
                }

                if (playerPres.IdleBobSpeed > 0f)
                    Debug.Log($"[Sevenfold] PASS: Player idle bob speed valid ({playerPres.IdleBobSpeed} > 0)");
                else
                {
                    Debug.LogError($"[Sevenfold] FAIL: Player idle bob speed must be > 0, was {playerPres.IdleBobSpeed}");
                    failures++;
                }
            }

            // Input config checks
            if (inputConfig == null)
            {
                Debug.LogError($"[Sevenfold] FAIL: InputConfigSO not found at {InputConfigPath}");
                failures++;
            }
            else
            {
                if (inputConfig.RollBindings != null && inputConfig.RollBindings.Length > 0)
                    Debug.Log("[Sevenfold] PASS: Input roll bindings configured.");
                else
                {
                    Debug.LogError("[Sevenfold] FAIL: Input roll bindings missing or empty.");
                    failures++;
                }

                if (inputConfig.UpBindings != null && inputConfig.UpBindings.Length > 0 &&
                    inputConfig.DownBindings != null && inputConfig.DownBindings.Length > 0 &&
                    inputConfig.LeftBindings != null && inputConfig.LeftBindings.Length > 0 &&
                    inputConfig.RightBindings != null && inputConfig.RightBindings.Length > 0)
                {
                    Debug.Log("[Sevenfold] PASS: Movement directional bindings configured (Up/Down/Left/Right).");
                }
                else
                {
                    Debug.LogError("[Sevenfold] FAIL: One or more movement directional bindings are missing or empty.");
                    failures++;
                }
            }

            // Player prefab checks
            if (playerPrefab == null)
            {
                Debug.LogError($"[Sevenfold] FAIL: Player prefab not found at {PlayerPrefabPath}");
                failures++;
            }
            else
            {
                var mover = playerPrefab.GetComponent<PlayerGridMover>();
                if (mover != null)
                {
                    Debug.Log("[Sevenfold] PASS: Player prefab has PlayerGridMover component.");
                    var moverSo = new SerializedObject(mover);
                    if (moverSo.FindProperty("_presentation").objectReferenceValue != null)
                    {
                        Debug.Log("[Sevenfold] PASS: Player prefab PlayerGridMover has PlayerPresentationSO assigned.");
                    }
                    else
                    {
                        Debug.LogError("[Sevenfold] FAIL: Player prefab PlayerGridMover missing PlayerPresentationSO reference.");
                        failures++;
                    }

                    if (moverSo.FindProperty("_visualChild").objectReferenceValue != null)
                    {
                        Debug.Log("[Sevenfold] PASS: Player prefab PlayerGridMover has VisualChild Transform assigned.");
                    }
                    else
                    {
                        Debug.LogError("[Sevenfold] FAIL: Player prefab PlayerGridMover missing VisualChild reference.");
                        failures++;
                    }
                }
                else
                {
                    Debug.LogError("[Sevenfold] FAIL: Player prefab is missing PlayerGridMover component.");
                    failures++;
                }
            }

            // Dice materials check
            var ivoryMat = AssetDatabase.LoadAssetAtPath<Material>(DiceIvoryMaterialPath);
            var pipMat = AssetDatabase.LoadAssetAtPath<Material>(DicePipMaterialPath);
            if (ivoryMat != null)
            {
                Debug.Log("[Sevenfold] PASS: Dice ivory material found.");
            }
            else
            {
                Debug.LogError($"[Sevenfold] FAIL: Dice ivory material missing at {DiceIvoryMaterialPath}");
                failures++;
            }

            if (pipMat != null)
            {
                Debug.Log("[Sevenfold] PASS: Dice pip material found.");
            }
            else
            {
                Debug.LogError($"[Sevenfold] FAIL: Dice pip material missing at {DicePipMaterialPath}");
                failures++;
            }

            if (failures == 0)
            {
                Debug.Log("<color=green>[Sevenfold] All data validated successfully! 0 errors.</color>");
            }
            else
            {
                Debug.LogError($"<color=red>[Sevenfold] Data validation finished with {failures} failure(s).</color>");
            }
        }

        [MenuItem("Sevenfold/Tests/Run EditMode Tests")]
        public static void RunEditModeTests()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter
            {
                testMode = TestMode.EditMode
            };
            api.Execute(new ExecutionSettings(filter));
        }

        private static void UpdateEditorBuildSettings()
        {
            var buildScenes = new List<EditorBuildSettingsScene>();
            if (File.Exists(MainMenuScenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));
            }
            if (File.Exists(GameplayScenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(GameplayScenePath, true));
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        private static void EnsureDataDirectories()
        {
            EnsureFolder("Assets/_Sevenfold", "Data");
            EnsureFolder("Assets/_Sevenfold/Data", "Config");
            EnsureFolder("Assets/_Sevenfold/Data", "Tiles");
            EnsureFolder("Assets/_Sevenfold/Data", "Boards");
            EnsureFolder("Assets/_Sevenfold/Data", "Blessings");
            EnsureFolder("Assets/_Sevenfold", "Art");
            EnsureFolder("Assets/_Sevenfold/Art", "Materials");
            EnsureFolder("Assets/_Sevenfold/Art", "Models");
            EnsureFolder("Assets/_Sevenfold", "Prefabs");
            EnsureFolder("Assets/_Sevenfold/Prefabs", "Player");
        }

        private static void EnsureFolder(string parentFolder, string newFolder)
        {
            string path = $"{parentFolder}/{newFolder}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parentFolder, newFolder);
            }
        }

        private static Material GetOrCreateTileMaterial()
        {
            EnsureFolder("Assets/_Sevenfold", "Art");
            EnsureFolder("Assets/_Sevenfold/Art", "Materials");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(TileMaterialPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                mat = new Material(shader) { name = "M_Tile_Default" };
                AssetDatabase.CreateAsset(mat, TileMaterialPath);
                AssetDatabase.SaveAssets();
            }
            return mat;
        }

        private static Material GetOrCreateMaterial(string path, string name, string colorHex)
        {
            EnsureFolder("Assets/_Sevenfold", "Art");
            EnsureFolder("Assets/_Sevenfold/Art", "Materials");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                mat = new Material(shader) { name = name };
                if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
                {
                    mat.color = color;
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", color);
                    }
                }
                AssetDatabase.CreateAsset(mat, path);
                AssetDatabase.SaveAssets();
            }
            return mat;
        }

        private static Mesh GetOrCreateConeMesh()
        {
            EnsureFolder("Assets/_Sevenfold", "Art");
            EnsureFolder("Assets/_Sevenfold/Art", "Models");

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ConeHatMeshPath);
            if (mesh == null)
            {
                mesh = CreateConeMesh(0.2f, 0.35f, 16);
                AssetDatabase.CreateAsset(mesh, ConeHatMeshPath);
                AssetDatabase.SaveAssets();
            }
            return mesh;
        }

        private static Mesh CreateConeMesh(float radius, float height, int segments)
        {
            var mesh = new Mesh { name = "PlayerHat_Cone" };

            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            // Apex
            vertices.Add(new Vector3(0f, height, 0f)); // Index 0

            // Base ring
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                vertices.Add(new Vector3(x, 0f, z)); // Index 1 + i
            }

            // Base center
            int baseCenterIndex = vertices.Count;
            vertices.Add(Vector3.zero); // Index segments + 1

            // Cone side triangles (Apex, next, current)
            for (int i = 0; i < segments; i++)
            {
                int current = 1 + i;
                int next = 1 + ((i + 1) % segments);
                triangles.Add(0);
                triangles.Add(next);
                triangles.Add(current);
            }

            // Base cap triangles (Center, current, next)
            for (int i = 0; i < segments; i++)
            {
                int current = 1 + i;
                int next = 1 + ((i + 1) % segments);
                triangles.Add(baseCenterIndex);
                triangles.Add(current);
                triangles.Add(next);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreatePlayerPawnHierarchy(
            Transform parent,
            PlayerPresentationSO presentation,
            Material bodyMaterial,
            Material hatMaterial,
            Mesh coneMesh)
        {
            var root = new GameObject("PlayerPawn");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            var mover = root.AddComponent<PlayerGridMover>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            // Body (Capsule)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(visual.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null)
            {
                Object.DestroyImmediate(bodyCollider);
            }

            var bodyRenderer = body.GetComponent<MeshRenderer>();
            if (bodyMaterial != null)
            {
                bodyRenderer.sharedMaterial = bodyMaterial;
            }

            // Hat (Cone)
            var hat = new GameObject("Hat");
            hat.transform.SetParent(visual.transform, false);
            hat.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            hat.transform.localScale = Vector3.one;

            var filter = hat.AddComponent<MeshFilter>();
            filter.sharedMesh = coneMesh;

            var hatRenderer = hat.AddComponent<MeshRenderer>();
            if (hatMaterial != null)
            {
                hatRenderer.sharedMaterial = hatMaterial;
            }

            var moverSo = new SerializedObject(mover);
            moverSo.FindProperty("_presentation").objectReferenceValue = presentation;
            moverSo.FindProperty("_visualChild").objectReferenceValue = visual.transform;
            moverSo.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static GameObject CreateOrUpdatePlayerPrefab(
            PlayerPresentationSO presentation,
            Material bodyMaterial,
            Material hatMaterial,
            Mesh coneMesh)
        {
            EnsureFolder("Assets/_Sevenfold", "Prefabs");
            EnsureFolder("Assets/_Sevenfold/Prefabs", "Player");

            var tempRoot = CreatePlayerPawnHierarchy(null, presentation, bodyMaterial, hatMaterial, coneMesh);
            var prefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, PlayerPrefabPath);
            Object.DestroyImmediate(tempRoot);
            return prefab;
        }

        private static DiceView CreateDiceObject(string name, Transform parent, Vector3 localPos, Material ivoryMat, Material pipMat, Vector3 spinAxis)
        {
            var dieGo = new GameObject(name);
            if (parent != null)
            {
                dieGo.transform.SetParent(parent, false);
            }
            dieGo.transform.localPosition = localPos;
            dieGo.transform.localRotation = Quaternion.identity;

            var diceView = dieGo.AddComponent<DiceView>();
            diceView.RestPosition = localPos;
            diceView.SpinAxis = spinAxis;

            // Cube body: 0.6 size
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(dieGo.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            var col = body.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var bodyRend = body.GetComponent<MeshRenderer>();
            if (ivoryMat != null) bodyRend.sharedMaterial = ivoryMat;

            // Pips root
            var pipsRoot = new GameObject("Pips");
            pipsRoot.transform.SetParent(dieGo.transform, false);
            pipsRoot.transform.localPosition = Vector3.zero;

            float d = 0.14f;
            float faceOffset = 0.301f;
            float pipScale = 0.08f;

            void AddPip(Vector3 offset)
            {
                var pip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pip.name = "Pip";
                pip.transform.SetParent(pipsRoot.transform, false);
                pip.transform.localPosition = offset;
                pip.transform.localScale = Vector3.one * pipScale;
                var pipCol = pip.GetComponent<Collider>();
                if (pipCol != null) Object.DestroyImmediate(pipCol);
                var pipRend = pip.GetComponent<MeshRenderer>();
                if (pipMat != null) pipRend.sharedMaterial = pipMat;
            }

            // Face 1 (+Y)
            AddPip(new Vector3(0f, faceOffset, 0f));

            // Face 6 (-Y)
            AddPip(new Vector3(-d, -faceOffset, -d));
            AddPip(new Vector3(-d, -faceOffset, 0f));
            AddPip(new Vector3(-d, -faceOffset, d));
            AddPip(new Vector3(d, -faceOffset, -d));
            AddPip(new Vector3(d, -faceOffset, 0f));
            AddPip(new Vector3(d, -faceOffset, d));

            // Face 2 (+Z)
            AddPip(new Vector3(-d, -d, faceOffset));
            AddPip(new Vector3(d, d, faceOffset));

            // Face 5 (-Z)
            AddPip(new Vector3(-d, -d, -faceOffset));
            AddPip(new Vector3(-d, d, -faceOffset));
            AddPip(new Vector3(d, -d, -faceOffset));
            AddPip(new Vector3(d, d, -faceOffset));
            AddPip(new Vector3(0f, 0f, -faceOffset));

            // Face 3 (+X)
            AddPip(new Vector3(faceOffset, -d, -d));
            AddPip(new Vector3(faceOffset, 0f, 0f));
            AddPip(new Vector3(faceOffset, d, d));

            // Face 4 (-X)
            AddPip(new Vector3(-faceOffset, -d, -d));
            AddPip(new Vector3(-faceOffset, -d, d));
            AddPip(new Vector3(-faceOffset, d, -d));
            AddPip(new Vector3(-faceOffset, d, d));

            var diceSo = new SerializedObject(diceView);
            diceSo.FindProperty("_restPosition").vector3Value = localPos;
            diceSo.FindProperty("_spinAxis").vector3Value = spinAxis;
            diceSo.FindProperty("_currentFace").intValue = 1;
            diceSo.ApplyModifiedPropertiesWithoutUndo();

            return diceView;
        }

        private static DebugHUD CreateDebugHUD(Transform parent, GameFlowController flow, TurnController turn)
        {
            var canvasGo = new GameObject("DebugCanvas");
            if (parent != null) canvasGo.transform.SetParent(parent, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Semi-transparent dark container panel
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(400f, 320f);

            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.11f, 0.16f, 0.88f);

            // Status Text
            var textGo = new GameObject("StatusText");
            textGo.transform.SetParent(panelGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 16f);
            textRect.offsetMax = new Vector2(-16f, -16f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.lineSpacing = 6;
            tmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            var debugHud = canvasGo.AddComponent<DebugHUD>();
            var hudSo = new SerializedObject(debugHud);
            hudSo.FindProperty("_statusText").objectReferenceValue = tmp;
            hudSo.FindProperty("_flowController").objectReferenceValue = flow;
            hudSo.FindProperty("_turnController").objectReferenceValue = turn;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // EventSystem with InputSystemUIInputModule
            var esGo = new GameObject("EventSystem");
            if (parent != null) esGo.transform.SetParent(parent, false);
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            return debugHud;
        }

        private static T GetOrCreateScriptableObject<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            return asset;
        }

        private static TileDefinitionSO SetupTile(string path, string stableId, string displayName, TileKind kind,
            string description, string colorHex, int effectAmount, bool isOneShot, bool hiddenUntilRevealed, string feedbackId)
        {
            var tile = GetOrCreateScriptableObject<TileDefinitionSO>(path);
            tile.StableId = stableId;
            tile.DisplayName = displayName;
            tile.TileKind = kind;
            tile.Description = description;
            if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
            {
                tile.BaseColor = color;
            }
            tile.EffectAmount = effectAmount;
            tile.IsOneShot = isOneShot;
            tile.HiddenUntilRevealed = hiddenUntilRevealed;
            tile.FeedbackEventId = feedbackId;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static FateBlessingDefinitionSO SetupBlessing(string path, BlessingKind kind, string title,
            string description, int revealRadius, int wardAmount, int rerollCount)
        {
            var blessing = GetOrCreateScriptableObject<FateBlessingDefinitionSO>(path);
            blessing.BlessingKind = kind;
            blessing.Title = title;
            blessing.Description = description;
            blessing.RevealRadius = revealRadius;
            blessing.WardAmount = wardAmount;
            blessing.RerollCount = rerollCount;
            EditorUtility.SetDirty(blessing);
            return blessing;
        }
    }
}
