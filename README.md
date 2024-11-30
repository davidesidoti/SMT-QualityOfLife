# SMT - QualityOfLife

![Steam](https://img.shields.io/badge/steam-%23000000.svg?style=for-the-badge&logo=steam&logoColor=white)
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![Unity](https://img.shields.io/badge/unity-%23000000.svg?style=for-the-badge&logo=unity&logoColor=white)
![Rider](https://img.shields.io/badge/Rider-000000.svg?style=for-the-badge&logo=Rider&logoColor=white&color=black&labelColor=crimson)

A BepInEx mod for the Unity game **Supermarket Together** that enhances your gameplay experience with quality-of-life features, aiming to simplify in-game tasks without making it feel like cheating.

## Features

### NPCAdder Mod

- **Extend NPC Limit**: Increase the maximum number of NPC employees in your store beyond the default limit, up to 15 NPCs.
- **Dynamic Management**: Add or remove NPC employees through an in-game interface.
- **Unlock Requirement**: Feature becomes available once all NPC-related upgrades are purchased in-game.

*Note: Currently, only the NPCAdder Mod is enabled. Other features are coming soon.*

### Upcoming Features

- **LowCountProducts Mod**: Quickly restock low-count products by adding them to the shopping cart directly from the manager's blackboard.
- **TwentyCents Mod**: Replace the 25-cent coin at checkout with a 20-cent coin for smoother transactions.
- **SmartPrices Mod**: Automatically adjust product prices to the highest possible value to maximize income.

## Installation

1. **Install BepInEx**: Ensure you have BepInEx installed for **Supermarket Together**. If not, download it from the [BepInEx GitHub repository](https://github.com/BepInEx/BepInEx/releases).
2. **Download the Mod**: Get the latest version of **SMT - QualityOfLife** from the [releases page](https://github.com/davidesidoti/SMT-QualityOfLife/releases).
3. **Extract the Files**: Unzip the downloaded file.
4. **Move the Mod Files**: Place the `.dll` files into the `BepInEx/plugins` directory within your game folder.
5. **Run the Game**: Launch **Supermarket Together**. The mod will load automatically.

## Usage

- **Open Mod Menu**: Press `Ctrl + H` to toggle the main mod window.
- **Enable NPCAdder Mod**:
  - Open the main mod window.
  - Find the **NPCAdder Mod** section.
  - Check the box to enable the mod.
  - Click on **Mod Settings** to configure.
- **Adjust NPC Count**:
  - In the NPCAdder settings, use the **+ Add** button to increase or the **- Remove** button to decrease the maximum number of NPCs.
  - Changes will reflect in-game instantly.

## Configuration

- **Keyboard Shortcuts**: Modify key bindings in the `BepInEx/config/SMTQualityOfLife.cfg` file.
- **Settings**: All settings are accessible in-game through the mod's GUI.

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
