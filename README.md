# SMT - QualityOfLife

![Steam](https://img.shields.io/badge/steam-%23000000.svg?style=for-the-badge&logo=steam&logoColor=white)
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![Unity](https://img.shields.io/badge/unity-%23000000.svg?style=for-the-badge&logo=unity&logoColor=white)
![Rider](https://img.shields.io/badge/Rider-000000.svg?style=for-the-badge&logo=Rider&logoColor=white&color=black&labelColor=crimson)

A BepInEx mod for the Unity game **Supermarket Together** that enhances your gameplay experience with quality-of-life features, aiming to simplify in-game tasks without making it feel like cheating.

## Features

### LowCountProducts Mod

- **Quick Restock**: Adds an "Add Low Count Products" button to the manager's blackboard that automatically adds all low-stock products to your shopping cart.
- **Two Thresholds + Hotkey Toggle**: Configure independent Low (default 20) and High (default 60) thresholds; press `Ctrl + Y` in-game to flip between them (rebindable in `BepInEx/config/SMTQualityOfLife.cfg` under `LowCountProducts ToggleThresholdHotkey`).
- **Total-Stock Check**: Compares shelves + storage + unopened boxes against the active threshold, so already-stocked products aren't re-ordered.
- **Three Add Modes**: `Original` (legacy: skip products with backup stock, skip if already in cart), `One Box Per Click` (adds 1 box per click, click again to add more), and `Auto-Fill` (computes the boxes needed to reach the threshold and adds them all in one click).
- **In-Window Toggle**: A "Switch to Low / Switch to High" button in the settings window mirrors the hotkey for players who prefer the GUI.

### SmartPrices Mod

- **Automatic Pricing**: When using the pricing gun, the price is automatically set to the optimal value based on a configurable markup percentage.
- **Formula**: `base price x tier inflation x (1 + markup / 100)`
- **Adjustable Markup**: Fine-tune your markup percentage through the in-game settings with +/- buttons.

### Checkout Volume

- **Scanner Beep Control**: Adjust the volume of the beep sound when products are scanned at checkout registers.
- **Works Everywhere**: Applies to both regular checkouts and self-checkout stations.
- **Slider + Presets**: Use a smooth slider or quick preset buttons (Mute, 25%, 50%, 75%, 100%).

## Installation

1. **Install BepInEx**: Ensure you have BepInEx installed for **Supermarket Together**. If not, download it from the [BepInEx GitHub repository](https://github.com/BepInEx/BepInEx/releases).
2. **Download the Mod**: Get the latest version of **SMT - QualityOfLife** from the [releases page](https://github.com/davidesidoti/SMT-QualityOfLife/releases).
3. **Extract the Files**: Unzip the downloaded file.
4. **Move the Mod Files**: Place the `.dll` files into the `BepInEx/plugins` directory within your game folder.
5. **Run the Game**: Launch **Supermarket Together**. The mod will load automatically.

## Usage

- **Open Mod Menu**: Press `Ctrl + H` to toggle the main mod window.
- **Enable Features**: Toggle each mod on/off from the main window.
- **Configure Settings**: Click "Mod Settings" next to any enabled mod to access its configuration.

## Configuration

- **Keyboard Shortcuts**: Modify key bindings in the `BepInEx/config/SMTQualityOfLife.cfg` file.
- **Settings**: All settings are accessible in-game through the mod's GUI and persist across sessions.

## Contributing

- **Bug Reports:** If you encounter any issues or bugs, please open an issue on GitHub.
- **Feature Requests:** Have an idea to improve the mod? Feel free to submit a feature request.

## License

This project is licensed under the GPL-3.0 License. See the [LICENSE](LICENSE) file for details.

## Credits

- **Author**: Davide Sidoti
- **Thanks to**:
  - The BepInEx team for their modding framework.
  - The Harmony library developers for enabling runtime patching.
