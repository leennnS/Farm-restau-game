# Intro Scene Atmospheric Polish - Implementation Guide

## Overview

This enhancement adds visual polish to the intro scene with atmospheric lighting and styled UI elements.

## Features Added

### 1. Styled Dialogue Box (TopDialoguePanel)

- Dark semi-transparent background positioned at top of screen
- Thin decorative border with rustic aesthetic
- Narrative text centered inside with warm off-white color
- Automatically wraps existing narrative text element

### 2. Styled Continue Prompt (ContinuePromptContainer)

- Positioned at bottom center of screen
- Semi-transparent background with decorative separators above/below text
- "Press Space to continue" styled with enhanced typography
- Integrates with existing hint text component

### 3. Window Light Streaks

- Two cool-toned spot lights (cyan/blue #4DB8E8)
- Positioned at window angles (top-left and top-right)
- Creates faint moonlight effect across floor
- Soft shadows for atmospheric quality
- Intensity: 0.3 and 0.18 (tunable via Inspector)

### 4. Character Overhead Light

- Warm-toned point light (#D8CCBE)
- Positioned 2.5 units above player character
- Gently separates character from background
- Creates subtle halo effect
- Intensity: 0.4 (tunable via Inspector)

## Installation

### Automatic Setup (Recommended)

1. Open the Intro scene: `Assets/Scenes/Main/Intro.unity`
2. In menu bar, select: **Window > Intro Scene Setup > Apply Atmospheric Polish**
3. The setup utility will:
   - Add IntroUIPolish component to NarrativeCanvas
   - Create IntroLighting GameObject with lights
   - Auto-assign component references
   - Save the scene

### Manual Setup

If automatic setup doesn't work:

1. **Add IntroUIPolish to NarrativeCanvas:**
   - Select the NarrativeCanvas GameObject in hierarchy
   - In Inspector, click "Add Component"
   - Search for and add "IntroUIPolish"
   - Script will auto-find text components

2. **Add IntroLighting to scene:**
   - Create a new empty GameObject at scene root
   - Name it "IntroLighting"
   - In Inspector, click "Add Component"
   - Search for and add "IntroLighting"
   - If available, assign "Main Character Variant" to the playerCharacter field

## Scene Hierarchy

After setup, your Intro scene hierarchy will be:

```
Scene Root
├── IntroLighting (NEW)
│   ├── WindowLightStreaks (Spot Light)
│   ├── WindowLightStreaks_2 (Spot Light)
│   └── CharacterLightGlow (Point Light)
│
├── NarrativeCanvas
│   ├── IntroUIPolish (NEW COMPONENT)
│   ├── TopDialoguePanel (NEW - created by IntroUIPolish)
│   │   └── [Original narrative text moved here]
│   ├── ContinuePromptContainer (NEW - created by IntroUIPolish)
│   │   └── [Original hint text moved here]
│   └── ...other canvas elements
│
├── Main Character Variant
├── Tilemap
└── ...other scene objects
```

## Customization

### Adjust Light Intensity

1. Select IntroLighting GameObject
2. In Inspector, modify these values:
   - **windowLightIntensity**: Controls brightness of window streaks (default: 0.3)
   - **characterLightIntensity**: Controls overhead light brightness (default: 0.4)

### Adjust Dialogue Box Styling

Edit IntroUIPolish.cs in SetupDialoguePanel():

```csharp
// Background color (RGBA, 0-1)
panelBg.color = new Color(0.08f, 0.06f, 0.04f, 0.85f);

// Border color
outline.effectColor = new Color(0.35f, 0.25f, 0.15f, 0.6f);
```

### Adjust Continue Prompt Styling

Edit IntroUIPolish.cs in SetupContinuePrompt():

```csharp
// Background color
bgImage.color = new Color(0.1f, 0.08f, 0.06f, 0.5f);
```

## Removal

To remove the atmospheric polish:

1. Menu: **Window > Intro Scene Setup > Remove Atmospheric Polish**

Or manually:

- Delete the IntroLighting GameObject
- Remove IntroUIPolish component from NarrativeCanvas

## Compatibility

- Works with existing IntroNarrativeController
- Works with existing UI structure
- No modifications needed to narrative scripts
- All changes are additive (non-destructive)

## Files Modified

### New Scripts Created

- `Assets/Scripts/Systems/IntroUIPolish.cs` - UI styling component
- `Assets/Scripts/Systems/IntroLighting.cs` - Atmospheric lighting component
- `Assets/Editor/IntroSceneSetup.cs` - Setup utility (Editor only)

### Scene Changes

- Intro.unity will have new GameObjects and components added
- Original elements are preserved and enhanced

## Troubleshooting

**Issue: Text components not found**

- Solution: Manually assign narrativeText and hintText in IntroUIPolish Inspector

**Issue: Player character light not positioned correctly**

- Solution: Manually assign Main Character Variant to playerCharacter field in IntroLighting

**Issue: Lights not visible**

- Possible causes:
  - Lights too dim (increase intensity in Inspector)
  - Wrong lighting mode (check scene lighting settings)
  - Renderer not using URP/HDRP

**Issue: UI boxes look wrong**

- Solution: Check Canvas render mode (should be Overlay or Camera)
- Check that TextMeshPro is properly installed

## Performance Notes

- Two spot lights + one point light (minimal overhead)
- UI panels use simple Image components (efficient)
- All effects are soft-shadowed for performance
- No post-processing effects used
- Compatible with mobile platforms

## Technical Details

### Lighting Configuration

- **Window Lights**: Spot lights with 15m and 12m range
- **Character Light**: Point light with 5m range
- **Shadow Type**: Soft shadows on all lights
- **Light Mode**: Realtime (configurable in light components)

### UI Configuration

- **Dialogue Box**: 400x100 rect, top-center anchored
- **Continue Prompt**: 400x60 rect, bottom-center anchored
- **All elements**: Use VerticalLayoutGroup for responsive sizing

## Next Steps

1. Open the Intro scene
2. Run the setup: **Window > Intro Scene Setup > Apply Atmospheric Polish**
3. Play the scene and preview the atmospheric effect
4. Adjust light intensity and UI colors as desired
5. Test the full intro sequence with typewriter effect
