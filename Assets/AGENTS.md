# AGENTS.md — Sevenfold: The Fortune Path

> Put this file in the ROOT of the Unity project (next to the `Assets` folder).
> Also copy it as `CLAUDE.md` if your agent is Claude Code, or `.cursorrules` / `AGENTS.md` for Cursor / Codex.
> Every prompt tells the agent to read this file first. This file is the contract. If it conflicts with the long design document, THIS FILE WINS.

---

## 1. What we are building

A Unity 6 game jam game (theme: "Lucky Number"). Solo developer. Desktop WebGL is the submission; Windows is for local QA.

- One 7×7 grid board on a floating island. Coordinates X=0..6 (left→right), Y=0..6 (bottom→top).
- Each turn the player rolls 2 six-sided dice. The sum (2–12) is a **movement-point budget**.
- The player moves one cell at a time (Up/Down/Left/Right only), spending 1 point per successful step. They may end the turn early. Unused points are lost.
- Tile effects resolve on **every step**, not only at the end.
- Collect 7 stars → the Fortune Gate opens → step into the gate → VICTORY.
- DEFEAT if hearts reach 0, or the last allowed turn ends without victory.
- If the **first roll of a turn** totals 7 → "Lucky Fate": game pauses, the player picks 1 of 3 blessings.

## 2. Locked technical decisions (do not change without being asked)

| Topic | Decision |
|---|---|
| Engine | Unity 6 (6000.x), **URP**, 3D |
| Input | **Input System package** (`UnityEngine.InputSystem`). Create `InputAction`s in code from `InputConfigSO`. No legacy `Input.GetKey`. |
| UI | **uGUI + TextMeshPro** (`TMPro`). No UI Toolkit. |
| Tweens | Coroutines only. **No DOTween**, no other packages unless the prompt says so. |
| Physics | No Rigidbody dice. Dice values are generated first, then animated to show that face. |
| Scenes | `Assets/_Sevenfold/Scenes/MainMenu.unity` (index 0) and `Assets/_Sevenfold/Scenes/Gameplay.unity` (index 1). No Boot scene. |
| Audio | No AudioMixer. `AudioService` with 1 music `AudioSource` + a pool of 6 SFX `AudioSource`s; volumes from `SettingsStore` (PlayerPrefs). |
| VFX | Built-in `ParticleSystem` only. No VFX Graph, no custom shaders. |
| Persistence | Only settings in PlayerPrefs. No save game, no network, no analytics. |
| Namespaces | `Sevenfold.Core`, `Sevenfold.Board`, `Sevenfold.Player`, `Sevenfold.Dice`, `Sevenfold.Luck`, `Sevenfold.UI`, `Sevenfold.Audio`, `Sevenfold.Feedback`, `Sevenfold.EditorTools`, `Sevenfold.Tests` |

## 3. Assembly definitions

- `Assets/_Sevenfold/Scripts/Sevenfold.Runtime.asmdef` — references: `Unity.InputSystem`, `Unity.TextMeshPro`, `UnityEngine.UI`.
- `Assets/_Sevenfold/Editor/Sevenfold.Editor.asmdef` — Editor platform only; references `Sevenfold.Runtime`, `Unity.TextMeshPro`, `UnityEngine.UI`, `Unity.InputSystem`.
- `Assets/_Sevenfold/Tests/EditMode/Sevenfold.Tests.EditMode.asmdef` — Editor only, test assembly; references `Sevenfold.Runtime`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`; `"overrideReferences": true`, `"precompiledReferences": ["nunit.framework.dll"]`, `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`.

## 4. Folder layout

```
Assets/
  _Sevenfold/
    Scripts/  Core/ Board/ Player/ Dice/ Luck/ UI/ Audio/ Feedback/
    Editor/            (editor-only tools: setup, scene builder, validators, build scripts)
    Tests/EditMode/
    Data/     Config/ Boards/ Tiles/ Blessings/ Feedback/ Themes/
    Prefabs/  Board/ Player/ Dice/ UI/ Effects/
    Art/      Materials/ Models/ Textures/ Animations/ VFX/
    Audio/    Music/ SFX/
    Scenes/
    Documentation/  CreditsAndAI.md  DevLog.md
  ThirdParty/<Publisher>/<PackName>/
```

## 5. Game rules — exact values (these are the defaults in GameConfigSO)

| Setting | Value |
|---|---|
| Board | 7 × 7 |
| Dice | 2 dice, 6 sides |
| Lucky number | 7 (only the FIRST roll of a turn can trigger it) |
| Stars required | 7 |
| Starting hearts | 3 |
| Starting charms | 1 |
| Max charms carried | 3 |
| Max turns | 7 |
| Auto end turn | Yes, automatically when movement points reach 0 (after a 0.3 s pause) |
| Cell spacing | 1.25 units; tile top 1.12 × 1.12 |
| Player move | 0.22 s per cell, hop height 0.12 |
| Dice anim | 0.7 s |
| Fate's Sight radius | 3 (Manhattan) |

### Board layout (default `BoardDefinitionSO`, stored as data, never hard-coded in runtime scripts)

| Object | Cells |
|---|---|
| Start | (0,0) |
| Fortune Gate | (6,6) |
| Stars (7) | (1,6) (5,6) (3,5) (0,4) (1,2) (3,2) (2,0) |
| Traps (4) | (4,5) (2,4) (1,1) (4,1) |
| Charms (2) | (1,5) (6,1) |
| Gaps (4) | (2,5) (4,4) (5,3) (3,1) |
| Everything else | Normal numbered tile |

### Tile behaviour

| Tile | On entry | After |
|---|---|---|
| Normal | nothing | — |
| Star | +1 star (first entry only) | becomes empty socket |
| Trap | if `WardCharges > 0`: `WardCharges -= 1`, no damage. Else `Hearts -= 1` | spent / neutral forever |
| Charm | if `Charms < MaxCharms`: `Charms += 1` and consume. Else show "Pouch full", leave it | — |
| Gap | can never be entered, no point spent | — |
| Gate | if locked: move is **rejected** (no point spent) + toast "Gate locked: X/7 stars". If open: VICTORY | — |

### Ward / charm rules

- `WardCharges` is an int (0..2).
- "Use Charm" (key C): allowed only when `Charms > 0` AND `WardCharges == 0` AND state is AwaitRoll or PlayerMoving. Effect: `Charms -= 1`, `WardCharges = 1`.
- Fortune Ward blessing: `WardCharges = min(WardCharges + 1, 2)`.
- Wards persist across turns until used.

### Hidden tiles (fog)

- Always visible: Start, Gate, Stars, Gaps, Normal tiles.
- Hidden until revealed: **Traps and Charms**. A hidden trap/charm looks exactly like a normal numbered tile.
- A cell is revealed permanently when the player is at Manhattan distance ≤ 1 from it (check after every step and at run start).
- Fate's Sight: permanently reveals every cell within Manhattan distance 3 of the player at the moment it is chosen.

### Lucky Fate blessings

| Blessing | Effect |
|---|---|
| Fate's Sight | Reveal radius 3 around player (permanent). Movement budget = 7. |
| Fortune Ward | `WardCharges +1` (max 2). Movement budget = 7. |
| Second Chance | Roll both dice again ONCE. Show both totals; player picks original 7 or the new total. The reroll can never trigger Lucky Fate. |

### Turn flow

1. `BeginTurn`: `TurnNumber += 1`, budget = 0, `luckyUsedThisTurn = false`. If `TurnNumber > MaxTurns` → DEFEAT (turn limit).
2. AwaitRoll → player presses Space / Roll button.
3. Rolling: `DiceService` generates values FIRST, then `DiceView` animates to them.
4. If first roll total == 7 → AwaitFateChoice → apply blessing.
5. PlayerMoving: each valid step: lock input → spend 1 point → animate → resolve tile → reveal neighbours → check win/lose → unlock input.
6. End turn (Enter / button / auto at 0 points) → back to 1.
7. Win check happens before the turn-limit check, so entering the gate on the last turn wins.

### Game states (enum `GameState`)

`MainMenu, TurnStart, AwaitRoll, Rolling, AwaitFateChoice, PlayerMoving, ResolvingTile, TurnEnding, Paused, Victory, Defeat`

Only `GameFlowController` changes the state. UI requests actions through methods; UI never sets state or `Time.timeScale` itself.

### Controls

| Action | Keys | When |
|---|---|---|
| Roll | Space | AwaitRoll |
| Move | WASD / Arrows | PlayerMoving |
| End turn | Enter | PlayerMoving |
| Use charm | C | see ward rules |
| Pause | Escape | any gameplay state except Victory/Defeat/Rolling |

## 6. Architecture rules (MUST follow)

1. **One runtime state object**: `GameSessionState` (plain C# class, NOT a ScriptableObject) holds TurnNumber, Hearts, StarsCollected, Charms, WardCharges, RemainingMovementPoints, LastDieA, LastDieB, LastRollTotal, GateOpen, PlayerCell (Vector2Int), CollectedPickupIds, ConsumedTrapIds, RevealedCells (HashSet<Vector2Int>), RunStartTime.
2. **ScriptableObjects are read-only at runtime.** Never write player progress into an SO. Copy starting values into `GameSessionState` at run start.
3. **Logic separate from visuals.** `DiceService` calculates, `DiceView` animates. `BoardGrid` validates, `BoardCellView` displays. Rule classes are plain C# where possible so EditMode tests can test them without a scene.
4. **UI shows state and sends requests.** It never owns values.
5. **Events**: C# `event Action<...>`. Subscribe in `OnEnable`, unsubscribe in `OnDisable`. No per-frame polling of managers.
6. **Wiring**: `[SerializeField] private` references, assigned by the editor scene builder. No `FindObjectOfType` / `FindFirstObjectByType` in Update. No singletons except `AudioService` (DontDestroyOnLoad, so music continues between scenes).
7. **No magic numbers** in gameplay scripts. Values come from `GameConfigSO`, `BoardDefinitionSO`, `TileDefinitionSO`, `FateBlessingDefinitionSO`, `PlayerPresentationSO`, `FeedbackLibrarySO`, `UIThemeSO`.
8. **Validate references** in `Awake` and log a clear `Debug.LogError` naming the missing field and GameObject.
9. One script = one responsibility. No giant GameManager.
10. **Restart must fully reset**: reload the Gameplay scene (`SceneManager.LoadScene`) — simplest and safest.
11. **Random**: only `DiceService` uses randomness, via `System.Random` with an optional fixed seed (`GameConfigSO.useFixedSeed`, `fixedSeed`) and an optional forced-rolls queue for tests. Log `Dice: A + B = S` in development builds.

## 7. The editor-automation rule (VERY IMPORTANT)

The developer should not have to wire things by hand. For every system you add:

- Generate assets with editor menu items under **`Sevenfold/…`** in `Assets/_Sevenfold/Editor/`.
- `Sevenfold/Setup/Create Or Update Default Data` — creates/updates all SO assets with the defaults above. Idempotent: running it twice must not duplicate anything.
- `Sevenfold/Setup/Build Gameplay Scene` — (re)builds the gameplay objects in `Gameplay.unity` and wires every serialized reference using `SerializedObject`/`SerializedProperty`. It ONLY deletes/recreates root objects named `__Generated_*`. It must NEVER touch `EnvironmentRoot` or any other object the developer placed by hand.
- `Sevenfold/Setup/Build Main Menu Scene` — same idea for the menu.
- `Sevenfold/Validate/Validate All Data` — runs the data validation checklist and logs PASS/FAIL per check.
- After each prompt, extend these builders so the new system appears automatically.
- Add both scenes to `EditorBuildSettings.scenes` in the correct order.

## 8. How to work (agent behaviour)

- Read this file before every task. Only do the current step; do not build future steps.
- Before writing code, list the files you will create/modify in 3–10 bullets.
- Write complete files. No `// TODO` placeholders in delivered code (unless the prompt says "stub").
- After coding, check for compile errors. If you can read the Unity log, read `%LOCALAPPDATA%\Unity\Editor\Editor.log` and search for `error CS`. Fix all errors before finishing.
- Write or update EditMode tests for every pure-logic rule you add.
- End every task with a report: **Files changed**, **Menu items the developer must click (in order)**, **How to test in Play Mode (step by step)**, **Known limitations**.
- Append 2–4 lines to `Assets/_Sevenfold/Documentation/DevLog.md` (date, step, what AI generated) — needed for the jam's AI disclosure.
- If something takes too long or is risky, choose the simpler option and say so.
- Never add a package, change render pipeline, or change rules in section 5 unless asked.
