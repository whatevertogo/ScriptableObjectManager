# ScriptableObject Data Management System

English | [简体中文](README.md)

A powerful Unity ScriptableObject data management tool with a visual editor interface to help you efficiently manage all ScriptableObject assets in your project.

## Features

### Core Features

- **Auto Scan & Register** - Automatically scan and register ScriptableObject types via `[ManagedData]` attribute
- **Category Management** - Group and manage assets by Category
- **Quick Creation** - Visual interface for quickly creating new ScriptableObject instances

### Query & Search

- **Real-time Search** - Filter assets by name or type in real-time
- **Advanced Search** - Multi-condition combined queries to quickly locate target assets
- **Type Filtering** - Filter assets by type or category

### Batch Operations

- **Multi-select Mode** - Select multiple assets for operations
- **Batch Edit** - Modify properties of multiple selected assets at once
- **Path Export** - Export all asset paths to a text file with one click

### Dependency Analysis

- **Reference Finder** - Find which objects reference the selected asset
- **Dependency Viewer** - Visualize asset dependency graph
- **Orphan Detection** - Find unused assets that aren't referenced by any object

### SOHelper Quick Edit

- **Quick Edit Window** - Edit asset properties in a standalone window
- **Inspector Integration** - Seamless integration with native Inspector
- **History Navigation** - Navigate forward/backward between SO references
- **Quick Button** - All ScriptableObject reference fields have a quick open button (🔍)

## Installation

1. Clone this project to your Unity project's `Assets/` directory:
   ```
   Assets/ScriptObjectManagerSystem/
   ```

2. Or use Unity Package Manager's Git URL feature (if pushed to GitHub):

## Usage

### Mark Managed Types

Add the `[ManagedData]` attribute to ScriptableObject classes you want to manage:

```csharp
using UnityEngine;

[ManagedData("Character Config")]
[CreateAssetMenu(fileName = "New Character", menuName = "Game/Character")]
public class CharacterSO : ScriptableObject
{
    public string displayName;
    public int maxHealth;
    // ...
}
```

### Open Management Window

In Unity Editor:
- Menu path: `Tools/SO Data Manager`
- Shortcut: `Ctrl + Shift + M`

### Window Functions

| Button | Description |
|--------|-------------|
| Scan | Re-scan all assets in the project |
| Create + | Create new ScriptableObject instance |
| Export Paths | Export all asset paths to text file |
| Find References | Find references of selected asset |
| Dependencies | View asset dependency graph |
| Orphans | View orphaned assets |
| Batch Edit | Batch edit selected assets |

## Project Structure

```
ScriptObjectManagerSystem/
├── Attribute/
│   └── ManagedDataAttribute.cs    # Managed data attribute
├── Editor/
│   ├── DataManagement/
│   │   ├── Core/                  # Core data structures
│   │   ├── Services/              # Business logic services
│   │   ├── UI/                    # Editor windows
│   │   └── SOQuickEditWindow.cs   # Quick edit window
│   └── SOHelper/                  # Helper tools
│       ├── GenericSOWindow.cs     # SO editor with history navigation
│       └── SOPopupDrawer.cs       # Quick open button drawer for SO references
└── ScriptObjectSO.cs              # Base class example
```

### SOHelper Module

SOHelper provides an enhanced ScriptableObject editing experience:

- **GenericSOWindow** - A generic SO editor window with history navigation, allowing quick jumps and returns between multiple SO references
- **SOPopupDrawer** - Automatically adds a quick open button (🔍) to all ScriptableObject reference fields, which opens the corresponding asset in the editor window

## Requirements

- Unity 2020.3 or higher
- No external third-party dependencies

## Contributing

Issues and Pull Requests are welcome!

## License

MIT License

## Author

Created with <3 for Unity developers
