# Idle Blacksmith RPG — Unity URP Mobile Idle Game

A polished casual **portrait** mobile idle-RPG: your blacksmith hauls ore, hammers swords,
stocks the display behind the counter, and customers walk up to the counter to buy.
Earn gold, buy upgrades, hire a helper, **expand your tiny roadside stall into a Grand Forge**,
and send adventurers on **idle dungeon expeditions** that pay out even while the app is closed.

**Target:** Unity **6000.3.20f1**, URP 17, portrait mobile (Android APK included, works standalone too).

---

## Quick start

1. Open this folder in Unity Hub (add as existing project) with Unity 6000.3.x.
2. On first open, the bootstrap generates **everything**: low-poly meshes, animations,
   animator controllers, URP pipeline, the `Shop` scene, and the full UI.
   Watch for `[IdleBlacksmith] BUILD OK` in the Console.
3. Press **Play**.

Regenerate any time: **Tools → Idle Blacksmith → Rebuild Everything**, then
**Tools → Idle Blacksmith → Open Shop Scene**.
**Tools → Idle Blacksmith → Capture Previews** renders portrait screenshots to `_Screenshots/`.
**Tools → Idle Blacksmith → Play-Mode Smoke Test** headlessly plays ~4s and exits 0 on pass.

Headless verification loop (used while building):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe" -batchmode -quit `
  -projectPath "<this folder>" -executeMethod IdleBlacksmith.EditorTools.EditorBoot.BuildAll | Out-Null
& "...\Unity.exe" -batchmode -projectPath "<this folder>" `
  -executeMethod IdleBlacksmith.EditorTools.EditorBoot.SmokeTest | Out-Null   # exits 0 on pass
& "...\Unity.exe" -batchmode -quit -projectPath "<this folder>" `
  -executeMethod IdleBlacksmith.EditorTools.PreviewBuilder.CaptureAll | Out-Null
```

Last run: build clean, 240 play-mode frames with zero game exceptions, 6 previews captured.

## Android APK (test on a phone)

Menu **Tools → Idle Blacksmith → Build Android APK**, or headless:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe" -batchmode -quit `
  -buildTarget Android -projectPath "<this folder>" `
  -executeMethod IdleBlacksmith.EditorTools.BuildAndroid.BuildApk | Out-Null
```

Output: `_Builds/IdleBlacksmith.apk` (debug-signed, IL2CPP, ARM64, GLES3,
min Android 8.0, **portrait**). Install by copying the APK to the phone (allow
"install unknown apps") or with `adb install _Builds/IdleBlacksmith.apk`.
For a Play-ready build, switch to a release keystore and enable AAB in
`Assets/_IdleBlacksmith/Editor/BuildAndroid.cs`.

## The game

- **Forge loop** — the smith picks ore → hammers at the anvil (progress bar + sparks) →
  stocks the sword display behind the counter.
- **Counter sales** — when there's stock, a customer walks the garden path, steps in through
  the front door, buys a sword at the counter (they never wander into the workshop), and leaves
  happy. Gold pops, the counter punches.
- **Upgrades** — Forge Mastery (craft faster), Swift Boots (move faster), Weapon Rack (+2 slots).
- **Helper** — hire a second blacksmith with his own apprentice anvil.
- **Shop growth (RPG progression)** — buy *Expand Shop* twice:
  Roadside Stall → Village Smithy → Grand Forge. Each tier is a visibly bigger building
  (wider floor, taller timbered walls, awning, glowing window, chimney, storefront flags,
  fences, lanterns, more flowers) and grants +2 rack slots and +15% sword prices.
- **Dungeon expeditions (idle RPG)** — send adventurers to the Goblin Caves (2 min),
  Ember Depths (8 min) or Dragon's Hoard (30 min). They keep going **while the app is closed**
  (UTC timestamps) and return with gold + **Relic Ore**, which permanently raises sword prices
  (+5% per ore, up to +200%). A red "!" badge marks a finished expedition.

## Launch flow

Splash screen (Replicate key art + emblem, tap to start) → one-time 3-page onboarding
(forge / counter / dungeon, illustrated) → game. The onboarding-seen flag is persisted.

## Save / load

Gold, upgrades, helper, rack stock, shop tier, relic ore, the running expedition and the
onboarding flag persist to
`%USERPROFILE%\AppData\LocalLow\CozyForge\Idle Blacksmith\idle_blacksmith_save.json`
(atomic JSON, autosave every 10s and on pause/quit). Old v1 saves load cleanly (new fields
fall back to defaults). Delete the file to reset.

## Project structure

```
Assets/_IdleBlacksmith/
  Scripts/
    Core/       GameManager, EconomyManager, UpgradeManager, ExpeditionManager,
                SaveSystem/SaveData, AudioManager, GameConfig
    Gameplay/   WorkerController, CustomerController, CustomerSpawner, OrePile, AnvilStation,
                SwordRack, SimpleWalker, LightFlicker, CameraBreath
    UI/         UIManager, GoldCounter, UpgradePanel, UpgradeRow, HelperCard, ExpandCard,
                DungeonPanel, DungeonRow, SplashScreen, OnboardingPanel, BouncyButton,
                FloatingText, WorldProgressBar, RackStockBar, SafeArea, PulseLoop
  Editor/       Code-driven asset pipeline (see below)
  Art/
    Icons/Raw/  Recraft UI icons (backgrounds removed, transparent)
    Menu/       Replicate flux-schnell art: splash, emblem, onboarding x3, dungeon header
  Animations/   Generated clips + controllers
  Prefabs/      Generated prefabs (characters, stations, props, 3 environment tiers, UI rows)
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
| `AssetFactory` | 8×8 palette texture (52 colors), materials, 9-slice UI sprites, icon + menu-art sprite import, Baloo 2 TMP fonts + TMP Settings |
| `AudioSynth` | WAV fallbacks for any missing SFX |
| `ModelFactory` / `WorldFactory` | Flat-shaded low-poly meshes (one palette = one art family), characters, stations, counter, and **three shop environment tiers** with vegetation/fences/lanterns |
| `AnimationFactory` | Idle/Walk/Carry/Hammer clips + AnimatorControllers |
| `SceneBuilder` | Shop scene: lighting, portrait camera, post volume, counter-based layout, waypoints, wiring, `GameConfig` |
| `UiBuilder` | Portrait HUD (gold + relic ore pills), bottom bar, upgrade/dungeon sheets, splash, onboarding, world bars |

## Asset & service provenance

- **3D models:** 100% procedural, one palette, one material — consistent by construction.
- **UI icons:** Recraft v3 (digital_illustration), post-processed by
  `Tools/RemoveWhiteBg.py` (Pillow flood-fill from the borders → transparent backgrounds).
  `IconFallback` draws stand-ins for any missing file.
- **Menu/splash/onboarding/dungeon art:** Replicate `black-forest-labs/flux-schnell` via
  `Tools/FetchMenuArt.ps1` (poll loop + PNG magic validation).
- **SFX:** ElevenLabs sound-generation (`hammer`, `coin`, `pop`, `upgrade`, `hire`, `denied`,
  `crackle` forge loop, `fanfare`).
- **Font:** Baloo 2 (SIL Open Font License).
- **PrimeTween** 1.4.11 (embedded in `Packages/`, Apache-2.0) drives all tweens.
- TextMeshPro for every piece of text (essentials auto-imported from the uGUI package).

## Tuning

Everything gameplay-related lives on **`Assets/_IdleBlacksmith/Settings/GameConfig.asset`**:
prices, timings, customer cadence, upgrade curves, expansion costs/bonuses, expedition
durations/rewards, relic-ore price bonus.
