# Farmory Chef

A relaxing 2D farm simulation and restaurant management game built with Unity.
Manage your crops, go fishing, raise animals, trade at the market, and run a
thriving restaurant—all within a cozy day-night cycle.

## Features

- **🌾 Farm Management** – Plant seeds, water crops, and harvest produce in a
  full crop lifecycle system
- **🎣 Fishing Minigame** – Test your timing skills to catch fish and other
  items with interactive gameplay
- **🐄 Animal Interactions** – Collect milk from cows and eggs from chickens
- **🏪 Market Trading** – Buy and sell items using an in-game economy and wallet
  system
- **🍽️ Restaurant Operations** – Prepare food at workstations and fulfill
  customer orders for rewards
- **⏰ Day-Night Cycle** – Progress through in-game days with an hour-based
  clock system
- **📦 Inventory System** – Manage and organize all your collected items
- **💾 Save & Load** – Preserve your progress and resume where you left off

## System Requirements

### Minimum Requirements

- **CPU:** Dual-core processor, 2.0 GHz or higher
- **RAM:** 2 GB
- **Graphics:** Integrated graphics with at least 256 MB VRAM
- **Display:** 1280x720 resolution or higher
- **Storage:** 500 MB available space
- **OS:** Windows 10+, macOS 10.12+, or Linux (Ubuntu 16.04+)

### Recommended Requirements

- **CPU:** Quad-core processor, 2.5 GHz or higher
- **RAM:** 4 GB or more
- **Graphics:** Dedicated graphics card
- **Display:** 1920x1080 or higher
- **Storage:** SSD with 1 GB available space
- **OS:** Windows 11, macOS 12+, or Ubuntu 20.04 LTS

## Installation & Setup

### Windows

1. **Download the Game**
   - Download the latest release from the [Releases](releases) page
   - Extract the .zip file to your preferred location

2. **Run the Game**
   - Open the extracted folder
   - Double-click `FarmoryChef.exe`
   - The game will launch automatically

3. **First-Time Setup**
   - The game will create a saves folder in `%AppData%\FarmoryChef\` on first
     run
   - Your save files will be stored here automatically

### macOS

1. **Download the Game**
   - Download the macOS release from the [Releases](releases) page
   - The file will typically download as `FarmoryChef.dmg`

2. **Install the Game**
   - Double-click the `.dmg` file to mount the disk image
   - Drag the **FarmoryChef** app to the **Applications** folder
   - Eject the disk image when complete

3. **Run the Game**
   - Open **Applications** and find **FarmoryChef**
   - Double-click to launch (you may see a security prompt the first time—click
     **Open** to proceed)
   - Grant any requested permissions

4. **Save File Location**
   - Save files are stored in: `~/Library/Application Support/FarmoryChef/`

### Linux (Ubuntu/Debian)

1. **Download the Game**
   - Download the Linux release from the [Releases](releases) page
   - Extract the `.tar.gz` file: `tar -xzf FarmoryChef.tar.gz`

2. **Install Dependencies** (if needed)

   ```bash
   sudo apt-get update
   sudo apt-get install libssl1.1 libx11-6 libxrandr2
   ```

3. **Run the Game**
   - Navigate to the extracted folder
   - Make the executable runnable: `chmod +x FarmoryChef.x86_64`
   - Run the game: `./FarmoryChef.x86_64`

4. **Save File Location**
   - Save files are stored in: `~/.config/unity3d/DefaultCompany/FarmoryChef/`

### Linux (Arch-based / Manjaro)

1. **Install Dependencies**

   ```bash
   sudo pacman -S libxrandr libx11 openssl-1.1
   ```

2. **Extract and Run**
   ```bash
   tar -xzf FarmoryChef.tar.gz
   cd FarmoryChef
   chmod +x FarmoryChef.x86_64
   ./FarmoryChef.x86_64
   ```

## Playing the Game

### Controls

- **Movement:** Arrow Keys or WASD
- **Interact:** Space or E
- **Select Hotbar Item:** Number Keys (1-9) or mouse click
- **Open Inventory:** I
- **Open Market:** M (when available)
- **Menu Navigation:** Mouse or Keyboard

### Getting Started

1. **First Day Tutorial** – Follow the in-game prompts to learn farming basics
2. **Plant Your First Crop** – Use seeds from your starting inventory
3. **Water & Harvest** – Advance time to grow crops, then harvest them
4. **Explore** – Visit the market to buy and sell items
5. **Expand** – Unlock fishing, animal interactions, and the restaurant as you
   progress

## Save & Load

- **Manual Save:** Your progress is auto-saved at the end of each in-game day
- **Load Game:** Select "Load" from the main menu to resume a previous save
- **Multiple Saves:** The game supports multiple save files for different
  playthroughs
- **Clear Saves:** Delete save files from your system's save directory to start
  fresh

### Save File Locations by OS

| OS          | Path                                                  |
| ----------- | ----------------------------------------------------- |
| **Windows** | `%AppData%\FarmoryChef\saves\`                        |
| **macOS**   | `~/Library/Application Support/FarmoryChef/saves/`    |
| **Linux**   | `~/.config/unity3d/DefaultCompany/FarmoryChef/saves/` |

## Troubleshooting

### Game Won't Launch

**Windows:**

- Ensure your graphics drivers are up to date
- Try running in Compatibility Mode (right-click → Properties → Compatibility)
- Disable fullscreen optimizations if enabled

**macOS:**

- Grant execute permissions:
  `chmod +x /Applications/FarmoryChef.app/Contents/MacOS/FarmoryChef`
- Check System Preferences → Security to allow the app to run

**Linux:**

- Ensure dependencies are installed: `ldd ./FarmoryChef.x86_64`
- Try running from terminal to see error messages: `./FarmoryChef.x86_64`

### Performance Issues

- Lower the resolution in the in-game settings menu
- Close background applications to free up system memory
- Update your GPU drivers
- On Linux, try using Proton if playing through Steam

### Save Files Not Loading

- Ensure the save file location has proper read/write permissions
- Check that your user account has access to the Application Data folder
- Try deleting corrupted save files and starting a new game

### Audio Not Working

- Check your system volume settings
- Ensure audio drivers are up to date
- On Linux, verify ALSA or PulseAudio is running

## Development

### Building from Source

If you want to build the game from source:

1. **Install Unity**
   - Download Unity Editor (version specified in
     `ProjectSettings/ProjectVersion.txt`)
   - Install through Unity Hub

2. **Clone the Repository**

   ```bash
   git clone <repository-url>
   cd Farm-restau-game
   ```

3. **Open in Unity**
   - Open Unity Hub → Add Project → Select this folder
   - Wait for the editor to load and import assets

4. **Build the Game**
   - File → Build Settings
   - Select your target platform (Windows, macOS, or Linux)
   - Click "Build" and choose an output location

### Project Structure

```
Assets/
├── Scripts/           # Core gameplay scripts
│   ├── Farming/      # Crop system
│   ├── Fishing/      # Fishing minigame
│   ├── Inventory/    # Item management
│   ├── Market/       # Trading system
│   ├── Resto/        # Restaurant operations
│   ├── Orders/       # Order fulfillment
│   ├── NPCs/         # Character behavior
│   ├── UI/           # User interface
│   ├── Systems/      # Global systems (money, time, save/load)
│   └── ...
├── Scenes/           # Game levels and menus
├── Prefabs/          # Reusable game objects
├── Art/              # Sprites and graphics
├── Audio/            # Music and sound effects
└── Resources/        # ScriptableObject configurations
```

## License

[Add your license information here]

## Credits

**Developed by:** Leen Samadi and Elise Trad  
**Date:** April 2026

## Support

For bugs, suggestions, or questions:

- Open an issue on GitHub
- Contact the development team

---

**Enjoy your farming and restaurant adventure in Farmory Chef!** 🌾🍽️
