# Emberforge — Idle Blacksmith · Unity URP Mobile Idle Game

A polished casual **portrait** mobile idle-RPG. You run a blacksmith **complex**: miners dig ore,
smiths hammer it into swords whose quality is rolled per blade, customers buy the best sword on
your rack, and adventurers raid dungeons while the app is closed. Spend the takings on five
upgradable buildings, permanent runes, and — when the forge is running at full tilt — burn the
whole run for ember shards and start again, permanently stronger.

The game opens with a four-card cinematic (the Ember choosing its new smith), a six-page
onboarding, and a title menu with Continue / New Game / Settings / About / Share / Rate.
Story dialogue between Bram, Petra, Sable, Sir Aldric and Nyx unfolds as the complex grows —
each beat plays exactly once and is saved, so the cast remembers where you are.

**Target:** Unity **6000.3.20f1**, URP 17, portrait mobile (Android APK included, works standalone too).

---

## Quick start

1. Open this folder in Unity Hub (add as existing project) with Unity 6000.3.x.
2. On first open, the bootstrap generates **everything**: low-poly meshes, animations,
   animator controllers, URP pipeline, the `Shop` scene, and the full UI.
   Watch for `[IdleBlacksmith] BUILD OK` in the Console.
3. Press **Play**.

Menu items: **Tools → Idle Blacksmith → Rebuild Everything** (full regenerate),
**Open Shop Scene**, **Capture Previews** (portrait screenshots into `_Screenshots/`),
**Capture Complex Stages** (the complex at building levels 1/3/5), and
**Play-Mode Smoke Test** (headless play-mode verification, exits 0 on pass).

---

## The game loop

```
Ore Mine ──ore──▶ Smithy (anvil) ──swords──▶ Weapon Rack ──sales──▶ Customers
    ▲                                             │                      │
    └──────────── gold buys buildings/upgrades ◀───┴──────┬───────────────┘
                                                          ▼
                              Dungeon Gate ──▶ relic ore ──▶ Sanctum runes
                                                          │
                                    Ember Shards ◀── Rekindle (prestige) ──▶ Talents
```

**Ore is a real resource.** The smith prospects 0.45/s on his own and the Mine adds more, so the
loop can slow down when you overspend but can never deadlock. When the pile runs dry the smith
waits at the ore heap with a "No ore!" bubble until a chunk is ready.

**Every sword is an item.** Six recipes (copper → dragonsteel) each cost different ore, take
different time and sell for different gold. When a craft completes the quality is rolled:

| Rarity | Price multiplier | Base odds | Gem colour |
|---|---|---|---|
| Common | ×1.0 | 62% | steel |
| Uncommon | ×1.5 | 22% | green |
| Rare | ×2.6 | 10% | blue |
| Epic | ×4.5 | 4.5% | purple |
| Legendary | ×9.0 | 1.5% | gold |

Luck (upgrade + Rune of Fortune + talents + achievements) raises the weight of every tier above
Common, so high-luck forges tilt toward Epic and Legendary without ever making them guaranteed.
Customers always buy the **best** sword on the rack first.

---

## The complex

Five buildings, each upgradable from unbuilt (level 0) to level 5. Every level visibly changes the
3D model, and the camera pulls back and re-centres as the yard fills in.

| Building | Starts | Effect per level |
|---|---|---|
| **The Smithy** | 1 | +2 rack slots, +15% prices, unlocks recipes |
| **Ore Mine** | 0 | +0.6 ore/s, +25 ore storage, unlocks silver/mithril/dragonsteel |
| **Trading Post** | 0 | −7% customer interval, +6% prices |
| **Dungeon Gate** | 0 | +1 concurrent expedition, +15% expedition rewards, unlocks deeper dungeons |
| **Enchanter's Sanctum** | 0 | +4 max rune level, −8% rune cost |

**Runes** (bought with relic ore at the Sanctum) are the sink that keeps dungeon currency useful
after the price bonus caps out: Flame (+6% prices/level), Haste (−3% craft time), Fortune
(+1 luck), Wealth (+10% ore/s).

---

## Progression & meta

- **Quest chain** (27 steps) — one objective at a time, claimed automatically the moment it is met.
  The HUD ticker always shows the current goal and its progress, so there is never any doubt about
  what to do next. The chain walks you from "forge 5 swords" all the way to the first rekindle.
- **Achievements** (24) — lifetime milestones that each leave a **small permanent bonus** on the
  forge (+% gold, ore, prices, forge speed, luck, offline rate).
- **Offline progress** — the mine keeps digging and the shop keeps selling while the app is closed.
  Coming back shows a "Welcome back" sheet with the payout (base 50% rate, raised by the Night
  Shift talent) and any expeditions that finished. Capped at 8 hours.
- **Prestige** — *Rekindle the Forge* burns gold, ore, buildings, upgrades, runes and the rack in
  exchange for ember shards, and buys **talents** that survive every reset:

  | Talent | Effect per level |
  |---|---|
  | Ember Heat | +10% gold from sales |
  | Deep Veins | +15% ore mined |
  | Quick Hands | −5% forge time |
  | Silver Tongue | +8% sword prices |
  | Lucky Strike | +1.5 luck |
  | Night Shift | +7% offline rate |
  | Master Merchant | +10% customer frequency |
  | Rich Veins | +20 ore storage |
  | Guild Contacts | −8% expedition duration |
  | Rune Mastery | +12% rune power |

  Kept through prestige: talents, shards, quest progress, achievements and every lifetime stat.

---

## Menu, splash and onboarding

Launch flow is **splash → title screen → onboarding (once) → game**. The title screen is an overlay
on the live Shop scene rather than its own scene, so the forge keeps working behind it and the build
settings stay a single scene.

- **Main menu:** emblem, title, progress summary, PLAY / SETTINGS / CREDITS.
- **Settings:** mute, SFX volume slider, haptics toggle, credits, *Reset Save* (two-step confirm),
  and a link back to the title screen.
- **Onboarding:** five illustrated pages (forge, mine, market, dungeon, prestige) with page dots.

The **HUD** is: three resource pills top-left (gold, ore + rate, relic ore), a five-button rail
top-right (menu, upgrades, achievements, prestige, mute), the quest ticker, and four sheet buttons
along the bottom (COMPLEX / FORGE / DUNGEON / QUEST). Android **back** closes the top sheet instead
of quitting.

---

## Save / load

Everything persists to
`%USERPROFILE%\AppData\LocalLow\CozyForge\Idle Blacksmith\idle_blacksmith_save.json`
(atomic JSON, autosave every 10s and on pause/quit). Delete the file to reset, or use
Settings → Reset Save.

Save schema is **v3**. Older v1/v2 saves are migrated on load: the three named upgrade levels become
a keyed upgrade list, the shop tier becomes the Smithy building level, and the bare sword count
becomes a list of real items.

---

## Project structure

```
Assets/_IdleBlacksmith/
  Scripts/
    Core/       GameManager, EconomyManager, ResourceManager, BuildingManager, RecipeManager,
                UpgradeManager, RuneManager, TalentManager, PrestigeManager, QuestManager,
                AchievementManager, Production, ExpeditionManager, OfflineProgress,
                SaveSystem/SaveData, GameConfig, Ids, ItemTypes, Goals, AudioManager
    Gameplay/   WorkerController, CustomerController, CustomerSpawner, OrePile, AnvilStation,
                SwordRack, SimpleWalker, BuildingVisuals, BuildingFlourish, CameraDirector,
                SwordVisuals, LightFlicker, CameraBreath
    UI/         UIManager (screen router), ComplexPanel, BuildingRow, ForgePanel, RecipeCard,
                RunePanel, RuneRow, DungeonPanel, DungeonRow, QuestPanel, HudTicker,
                MetaPanel, AchRow, PrestigePanel, TalentRow, SettingsPanel, MainMenuPanel,
                WelcomeBackPanel, UpgradePanel, UpgradeRow, HelperCard, ExpandCard,
                SplashScreen, OnboardingPanel, GoldCounter, BouncyButton, FloatingText,
                WorldProgressBar, RackStockBar, SafeArea, PulseLoop
  Editor/       Code-driven asset pipeline (see below)
  Art/
    Icons/Raw/  Recraft UI icons (backgrounds removed, transparent)
    Menu/       Replicate flux-schnell art: splash, menu, emblem, onboarding pages, dungeon
  Animations/   Generated clips + controllers
  Prefabs/      Generated prefabs (characters, stations, props, 5 shop tiers, 20 building
                levels, 6 recipe swords, UI row templates)
  Audio/        ElevenLabs SFX (WAV synth fallback if a download failed)
  Fonts/        Baloo 2 (OFL) + generated TMP font assets
  Scenes/       Shop.unity (generated)
  Settings/     GameConfig, URP asset, post profile
```

### Code-driven asset pipeline (`Editor/`)

`EditorBoot.BuildAll()` runs the whole chain — idempotent, safe to re-run:

| Step | What it does |
|---|---|
| `ProjectSetup` | Linear color space, **portrait** orientation, creates + assigns the URP asset |
| `IconFallback` | Draws a flat stand-in for any icon the download missed |
| `AssetFactory` | 8×8 palette texture (52 colours), materials, 9-slice UI sprites, sprite import, Baloo 2 TMP fonts + settings |
| `AudioSynth` | WAV fallbacks for any missing SFX |
| `ModelFactory` | Characters, stations, props, the 5 shop tiers, and `SwordFactory` builds the 6 recipe swords + rarity gem materials + sparkle FX |
| `BuildingFactory` | The other four complex buildings, 5 levels each, with lights and particle flourishes |
| `AnimationFactory` | Idle/Walk/Carry/Hammer clips + AnimatorControllers |
| `SceneBuilder` | Shop scene: lighting, portrait camera, post volume, layout, the four building plots, waypoints, all systems, wiring, `GameConfig` (including the quest/achievement/rune/talent tables) |
| `UiBuilder` | Portrait HUD, all sheets, splash, main menu, onboarding, world bars, row prefabs |

Layout convention worth knowing: `Box(name, parent, anchor, pivot, pos, size)` collapses the anchors
so `sizeDelta` is literal pixels, and full-bleed elements use `StretchBox`. Masked art uses
`CoverFitter`, which assigns the sprite's own aspect ratio *before* switching the
`AspectRatioFitter` to `EnvelopeParent` — without that ordering the editor bakes the container's
ratio into the component and the sprite renders stretched.

---

## Asset & service provenance

- **3D models:** 100% procedural, one palette, one material — consistent by construction.
- **UI icons:** Recraft v3 (`digital_illustration`), post-processed by
  `Tools/RemoveWhiteBg.py` (Pillow flood-fill from the borders → transparent backgrounds).
  `IconFallback` draws stand-ins for any missing file.
- **Menu/splash/onboarding/dungeon art:** Replicate `black-forest-labs/flux-schnell` via
  `Tools/FetchMenuArt.ps1` (poll loop + PNG magic validation). Supports `-Force` and `-Only <name>`.
- **SFX:** ElevenLabs sound-generation (`hammer`, `coin`, `pop`, `upgrade`, `hire`, `denied`,
  `crackle` forge loop, `fanfare`, `mine_pick`, `market_chime`, `enchant`, `quest_done`,
  `achievement`, `prestige`, `unlock`, `levelup`, `whoosh`).
  `Tools/FetchAssets.ps1` also honours `-Force` / `-Only`.
- **Font:** Baloo 2 (SIL Open Font License).
- **PrimeTween** 1.4.11 (embedded in `Packages/`, Apache-2.0) drives all tweens.
- TextMeshPro for every piece of text (essentials auto-imported from the uGUI package).

The fetch scripts read `Game Dev Tools.txt` (gitignored) for API keys, falling back to
`REPLICATE_API_TOKEN` / `ELEVENLABS_API_KEY`. Nothing is hard-coded, and every generator has an
offline fallback so a failed download never breaks a build.

---

## Verification

```powershell
$U = "C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe"
& $U -batchmode -quit -projectPath "<this folder>" `
     -executeMethod IdleBlacksmith.EditorTools.EditorBoot.BuildAll | Out-Null
& $U -batchmode -projectPath "<this folder>" `
     -executeMethod IdleBlacksmith.EditorTools.EditorBoot.SmokeTest          # exit 0 = pass
& $U -batchmode -quit -projectPath "<this folder>" `
     -executeMethod IdleBlacksmith.EditorTools.PreviewBuilder.CaptureAll | Out-Null
& $U -batchmode -quit -projectPath "<this folder>" `
     -executeMethod IdleBlacksmith.EditorTools.PreviewBuilder.CaptureComplexStages | Out-Null
```

The smoke test enters play mode, runs 15 seconds of **game time** (measured with `Time.time`, since
batchmode ticks `EditorApplication.update` far faster than the player loop), and asserts: every
gameplay system exists, ore production is positive, ore actually moves, at least one sword is
forged, and there are zero game exceptions. It then prints a full state dump.

---

## Android APK (test on a phone)

Menu **Tools → Idle Blacksmith → Build Android APK**, or headless:

```powershell
& $U -batchmode -quit -buildTarget Android -projectPath "<this folder>" `
     -executeMethod IdleBlacksmith.EditorTools.BuildAndroid.BuildApk
```

Output: `_Builds/IdleBlacksmith.apk` (debug-signed, IL2CPP, ARM64, GLES3,
min Android 8.0, **portrait**). Install by copying the APK to the phone (allow
"install unknown apps") or with `adb install _Builds/IdleBlacksmith.apk`.
For a Play-ready build, switch to a release keystore and enable AAB in
`Assets/_IdleBlacksmith/Editor/BuildAndroid.cs`.

---

## Tuning

Everything gameplay-related lives on **`Assets/_IdleBlacksmith/Settings/GameConfig.asset`**:
ore rates and capacity, sword prices and craft times, customer cadence, upgrade curves, building
costs and effects, expedition durations/rewards/unlock tiers, rarity weights (via `RarityInfo`),
rune and talent effects, prestige payout formula, offline rate and cap.

The quest and achievement tables live in `SceneBuilder.BuildQuests()` / `BuildAchievements()`
so the whole chain can be read in one place and is regenerated with the scene.
