# Intro Scene Architecture - Visual Guide

## System Overview Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    GAME STARTUP FLOW                            │
└─────────────────────────────────────────────────────────────────┘

MENU (or GameManager.NewGame())
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│                    INTRO SCENE LOADS                            │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Canvas (NarrativeCanvas)                                  │ │
│  │  ├─ TextMeshPro Text (NarrativeText)                      │ │
│  │  │   └─ Displays: "The sun rises..."                      │ │
│  │  └─ NarrativeManager [Script]                            │ │
│  │     ├─ Array: ["Str1", "Str2", "Str3", "Str4"]          │ │
│  │     ├─ Typewriter Effect (0.05 speed)                    │ │
│  │     └─ Input Handler (Space/Click)                       │ │
│  │                                                           │ │
│  │ Background: Black (Solid Color)                          │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  PlayerPrefs: FromIntroScene = 1 [SET]                        │
└─────────────────────────────────────────────────────────────────┘
    │
    │ User presses Space/Click repeatedly
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│           NARRATIVE SEQUENCES (Typewriter Effect)               │
│                                                                 │
│  Sentence 1: T─y─p─i─n─g─ ─o─u─t─ ─s─l─o─w─l─y...           │
│  [Press Space] ✓ Move to Sentence 2                           │
│                                                                 │
│  Sentence 2: M─o─r─e─ ─n─a─r─r─a─t─i─v─e─ ─t─e─x─t         │
│  [Press Space] ✓ Move to Sentence 3                           │
│                                                                 │
│  Sentence 3: Y─e─t─ ─a─n─o─t─h─e─r─ ─s─e─n─t─e─n─c─e     │
│  [Press Space] ✓ Move to Sentence 4                           │
│                                                                 │
│  Sentence 4: Press Space or Click to continue...              │
│  [Press Space] ✓ All done!                                    │
└─────────────────────────────────────────────────────────────────┘
    │
    │ NarrativeManager.TransitionToNextScene()
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│               FARMSCENE LOADS (SceneManager)                    │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ SpawnManager [Script]                                     │ │
│  │                                                           │ │
│  │ ▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬│
│  │ Read PlayerPrefs: FromIntroScene                           │ │
│  │ ▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬│
│  │                                                           │ │
│  │ Value = 1 ? YES                                           │ │
│  │    └─► Use shedDoorSpawnPoint                             │ │
│  │    └─► Player appears at door (position: 10, 2, 0)       │ │
│  │                                                           │ │
│  │ Value = 0 or missing ? NO                                │ │
│  │    └─► Use defaultSpawnPoint                             │ │
│  │    └─► Player appears at default (position: 0, 0, 0)     │ │
│  │                                                           │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Player GameObject                                              │
│  ├─ CharacterController2D [Auto-positioned by SpawnManager]   │ │
│  ├─ Rigidbody2D                                                │ │
│  └─ Animator                                                   │ │
│                                                                 │
│  Scene Objects                                                  │
│  ├─ ShedDoorSpawnPoint (Empty, position: 10, 2, 0)            │ │
│  ├─ DefaultSpawnPoint (Empty, position: 0, 0, 0)              │ │
│  ├─ Farm Tilemap                                               │ │
│  ├─ Trees, Crops, etc.                                         │ │
│  └─ ...Your Farm Content...                                    │ │
│                                                                 │
│  PlayerPrefs: FromIntroScene [CLEARED]                         │ │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
GAMEPLAY CONTINUES
```

---

## Intro Scene Component Hierarchy

```
Intro.unity Scene
│
├─ Main Camera
│  └─ Background: Solid Color (Black)
│
├─ EventSystem (auto-created)
│
└─ Canvas (NarrativeCanvas)
   ├─ Graphic Raycaster
   └─ NarrativeText (TextMeshProUGUI)
      └─ NarrativeManager [Component]
         ├─ Narrative Text: <ref to NarrativeText>
         ├─ Narrative Sequence[]: ["Text1", "Text2", ...]
         ├─ Typewriter Speed: 0.05
         └─ Scene To Load After: "FarmScene"
```

---

## FarmScene Component Hierarchy

```
FarmScene.unity Scene
│
├─ Main Camera
│
├─ SpawnManager [GameObject]
│  └─ SpawnManager [Component]
│     ├─ Shed Door Spawn Point: <ref to ShedDoorSpawnPoint>
│     ├─ Default Spawn Point: <ref to DefaultSpawnPoint>
│     ├─ Shed Door Spawn Point Name: "ShedDoorSpawnPoint"
│     └─ Default Spawn Point Name: "DefaultSpawnPoint"
│
├─ ShedDoorSpawnPoint [Empty GameObject]
│  └─ Position: (10, 2, 0)
│
├─ DefaultSpawnPoint [Empty GameObject]
│  └─ Position: (0, 0, 0)
│
├─ Player [GameObject]
│  ├─ CharacterController2D [Script]
│  │  └─ Gets positioned by SpawnManager
│  ├─ Rigidbody2D
│  ├─ Animator
│  └─ Sprite Renderer
│
├─ Farm Tilemap
│
└─ ... Other Scene Objects ...
```

---

## Input Flow Chart

```
┌─────────────────────┐
│  NarrativeManager   │
│     Update()        │
└──────────┬──────────┘
           │
           ▼
    ┌─────────────────┐
    │ Check Input:    │
    │ Space or Click? │
    └────────┬────────┘
             │
        YES  │  NO
      ┌──────┘  │
      ▼         ▼
    ┌─────────────────┐    ┌─────────────────┐
    │ Handle Input()  │    │ Continue Loop   │
    └────────┬────────┘    └─────────────────┘
             │
             ▼
    ┌─────────────────────────────┐
    │ isTyping = true?            │
    └────────┬────────────────────┘
             │
        YES  │  NO
      ┌──────┘  │
      ▼         ▼
  ┌─────────────────────┐   ┌──────────────────┐
  │ Stop Typewriter     │   │ isFinished = true?│
  │ Show Full Text      │   └─────────┬────────┘
  │ isTyping = false    │             │
  │ isFinished = true   │        YES  │  NO
  └─────────────────────┘      ┌──────┘  │
                               ▼         ▼
                        ┌──────────────────────┐
                        │ Move to Next Sentence│
                        │ (or Load Scene)      │
                        └──────────────────────┘
```

---

## Data Storage: PlayerPrefs

```
┌──────────────────────────────────────────────┐
│          PlayerPrefs Storage                  │
├──────────────────────────────────────────────┤
│ Key: "FromIntroScene"                        │
│ Value: 1 (when set by NarrativeManager)      │
│                                               │
│ Set by: NarrativeManager.Start()             │
│ Used by: SpawnManager.SpawnPlayer()          │
│ Cleared by: SpawnManager.SpawnPlayer()       │
│            (only works once per game start)   │
└──────────────────────────────────────────────┘
```

---

## Position Reference

```
FARMSCENE Layout Example:

        World Space (Y-axis up, X-axis right)
                │
         Z=0    │    (Front)
                │
     ┌──────────┼──────────┐
     │          │          │
     │          │          │
─────┼──────────●──────────┼─────  X-axis
     │    (0,0) │    (10,2)│
     │          │ ●        │
     │          │ Shed     │
     │     Farm │ Door     │
     │          │ Spawn    │
     │   Default│          │
     │   Spawn  │          │
     └──────────┼──────────┘

DefaultSpawnPoint: (0, 0, 0)
ShedDoorSpawnPoint: (10, 2, 0)  ← Adjust to your actual shed door location
```

---

## Sequence Diagram: From Start to Gameplay

```
Time →

Intro Scene Loads
  │
  ├─ NarrativeManager.Start()
  │  └─ Sets PlayerPrefs: FromIntroScene = 1
  │
  ├─ NarrativeManager.Update() [Loop]
  │  ├─ Input check (Space/Click)
  │  ├─ Typewriter effect renders
  │  └─ Advances through narrative array
  │
  └─ NarrativeManager.TransitionToNextScene()
     └─ SceneManager.LoadScene("FarmScene")
        │
        └─ FarmScene Loads
           │
           ├─ SpawnManager.Start()
           │  │
           │  ├─ Find CharacterController2D
           │  ├─ Check PlayerPrefs: FromIntroScene
           │  │  ├─ Is 1? → Use shedDoorSpawnPoint
           │  │  └─ Is 0? → Use defaultSpawnPoint
           │  │
           │  ├─ Set player.position = spawn location
           │  ├─ Clear PlayerPrefs: FromIntroScene
           │  └─ Debug log position
           │
           └─ Game Systems Initialize (DayNight, etc.)
              │
              └─ Game Ready!
```

---

## Sync Points: What Happens When

| Event           | Location                     | Action                             |
| --------------- | ---------------------------- | ---------------------------------- |
| Game Start      | Menu/GameManager             | Load "Intro" scene                 |
| Intro Loads     | NarrativeManager.Start       | Set PlayerPrefs (FromIntroScene=1) |
| Gameplay        | NarrativeManager.Update      | Show typewriter text, check input  |
| Complete        | NarrativeManager (when done) | LoadScene("FarmScene")             |
| FarmScene Loads | SpawnManager.Start           | Check PlayerPrefs, position player |
| Spawn Complete  | SpawnManager                 | Clear PlayerPrefs, begin gameplay  |
