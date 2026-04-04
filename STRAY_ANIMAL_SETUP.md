# Stray Animal System - Setup Guide

## Overview

The stray animal system spawns random animals (pigs, wolves, etc.) on your farm periodically. These animals:

- Wander randomly around the farm
- Destroy crops when they get close
- Despawn after a duration or can be killed by the player

## Files Created

1. **StrayAnimalDefinition.cs** - ScriptableObject for animal types
2. **StrayAnimalController.cs** - Behavior for individual animals
3. **StrayAnimalSpawner.cs** - Spawns animals periodically
4. **FarmingManager.cs** - Added `TryDestroyPlantedCrop()` method

## Setup Steps

### Step 1: Create Animal Definitions

1. In your Assets folder, right-click → Create → Farming → Stray Animal Definition
2. Name it (e.g., "StrayPig", "StrayWolf")
3. Configure settings:
   - **animalName**: Display name
   - **moveSpeed**: Movement speed (0.5-3)
   - **minWalkTime**: Minimum walk duration
   - **maxWalkTime**: Maximum walk duration
   - **cropDetectionRadius**: How close to crops before destroying them
   - **destructionChancePerSecond**: Probability of destroying crops
   - **lifeTimeDuration**: How long animal stays on farm (30-60 seconds recommended)
4. Optional: Assign a sprite for the animal

### Step 2: Create Stray Animal Prefab (Optional)

1. Create a simple GameObject with:
   - A SpriteRenderer component (optional if using definition sprite)
   - A CircleCollider2D (for click detection)
   - StrayAnimalController script attached
2. Save as prefab: `Assets/Prefabs/StrayAnimal` or similar

### Step 3: Setup Spawner in Scene

1. In your Farm scene, create an empty GameObject called "StrayAnimalSpawner"
2. Add the **StrayAnimalSpawner** script
3. Configure:
   - **availableAnimals**: Add your animal definitions (created in Step 1)
   - **maxActiveAnimals**: Max animals on farm at once (1-3 recommended)
   - **minSpawnIntervalSeconds**: Min time between spawns (30 seconds)
   - **maxSpawnIntervalSeconds**: Max time between spawns (60 seconds)
   - **spawnAreaCenter**: Reference the farm center transform
   - **spawnAreaSize**: Area where animals can spawn (20x20 default)
   - **strayAnimalPrefab**: Optional - prefab from Step 2

### Step 4: Verify References

- Make sure your FarmingManager is in the scene and assigned to the spawner
- StrayAnimalController will auto-find FarmingManager if not assigned

## How It Works

### Spawning

- Spawner picks a random animal definition every 30-60 seconds
- Creates new animal at random spawn position
- Max 2 animals active simultaneously

### Animal Behavior

- Wanders randomly in the spawn area
- Every second, checks for nearby crops within detection radius
- Has a chance to destroy each nearby crop
- After lifeTimeDuration, despawns naturally

### Player Interaction

- Click on an animal to kill it instantly
- Requires CircleCollider2D on the animal

### Crop Destruction

- When animal gets close to crops, they're destroyed
- Destroyed crops don't give harvest items
- Soil reverts to dirt after destruction
- Player sees visual feedback in game

## Customization

### Easy Difficulty

- Increase spawn interval (90-120 seconds)
- Decrease max active animals (1)
- Increase crop detection radius awareness

### Hard Difficulty

- Decrease spawn interval (20-30 seconds)
- Increase max active animals (3-4)
- Increase crop destruction chance
- Decrease animal lifetime so they stay longer

### Visual Customization

- Assign custom sprite in animal definition
- Change animal color
- Adjust movement speed

## Troubleshooting

**Animals not spawning?**

- Check that FarmingManager is in scene
- Verify at least one animal definition is assigned
- Check spawn interval isn't too large

**Crops not being destroyed?**

- Increase cropDetectionRadius
- Increase destructionChancePerSecond
- Verify animal is getting close to crops (use gizmo view)

**Animals not clickable?**

- Verify CircleCollider2D on animal
- Check collider is not set to "Is Kinematic"
- Make sure camera is set up for raycasting

## Example Setup

```
Farm Scene
├── FarmingManager (already exists)
├── StrayAnimalSpawner (new)
│   ├── availableAnimals[0]: StrayPig.asset
│   ├── availableAnimals[1]: StrayWolf.asset
│   ├── maxActiveAnimals: 2
│   └── spawnAreaCenter: FarmCenter transform
└── (Spawned Animals appear here at runtime)
```

## Events & Extensions

Current implementation is straightforward - extend as needed:

- **Add damage to player** if they touch animal
- **Add sound effects** for destruction/spawn
- **Add particle effects** on crop destruction
- **Add animal tracking UI** showing active predators
