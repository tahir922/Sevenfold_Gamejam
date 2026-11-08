using System.Collections.Generic;
using Sevenfold.Board;
using Sevenfold.Core;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Sevenfold.EditorTools
{
    public static class SevenfoldMenu
    {
        private const string RootDataPath = "Assets/_Sevenfold/Data";
        private const string ConfigPath = "Assets/_Sevenfold/Data/Config/GameConfig.asset";
        private const string BoardPath = "Assets/_Sevenfold/Data/Boards/Board_Main.asset";

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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Sevenfold] Default data assets created/updated successfully.");
        }

        [MenuItem("Sevenfold/Setup/Build Gameplay Scene")]
        public static void BuildGameplayScene()
        {
            Debug.Log("[Sevenfold] Build Gameplay Scene: not implemented yet");
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

                if (config.StartingHearts > 0)
                    Debug.Log($"[Sevenfold] PASS: Starting hearts valid ({config.StartingHearts} > 0)");
                else
                {
                    Debug.LogError($"[Sevenfold] FAIL: Starting hearts must be > 0, was {config.StartingHearts}");
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

        private static void EnsureDataDirectories()
        {
            EnsureFolder("Assets/_Sevenfold", "Data");
            EnsureFolder("Assets/_Sevenfold/Data", "Config");
            EnsureFolder("Assets/_Sevenfold/Data", "Tiles");
            EnsureFolder("Assets/_Sevenfold/Data", "Boards");
            EnsureFolder("Assets/_Sevenfold/Data", "Blessings");
        }

        private static void EnsureFolder(string parentFolder, string newFolder)
        {
            string path = $"{parentFolder}/{newFolder}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parentFolder, newFolder);
            }
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
