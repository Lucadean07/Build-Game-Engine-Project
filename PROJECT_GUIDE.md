# BuildEditor Project Guide

## Overview
BuildEditor is a C# MonoGame application that serves as a level editor for building game environments. It provides a comprehensive 2D/3D editing interface for creating sectors, walls, sprites, and managing player positioning.

## Project Structure
```
BuildEditor/
├── BuildEditor.sln          # Solution file
├── BuildEditor/
    ├── BuildEditor.csproj    # Project file
    ├── Program.cs           # Entry point
    └── Game1.cs             # Main game logic (300KB+ - contains all core functionality)
```

## Technology Stack
- **Framework**: .NET 8.0 (WinExe)
- **Game Engine**: MonoGame.Framework.DesktopGL 3.8.*
- **UI Framework**: Myra 1.5.9
- **JSON**: Newtonsoft.Json 13.0.3
- **Build Tools**: MonoGame.Content.Builder.Task 3.8.*

## Core Classes & Components

### Main Game Class
- **BuildLevelEditor** (`Game1.cs:13`) - Main game class inheriting from MonoGame's Game

### Core Data Structures
- **Sector** (`Game1.cs:6981`) - Represents game world sectors with floors, ceilings, and walls
- **Wall** (`Game1.cs:7058`) - Wall geometry and properties
- **Sprite** (`Game1.cs:6913`) - 3D sprites with positioning and alignment
- **Camera2D** (`Game1.cs:6869`) - 2D camera system for viewport management

### Enumerations
- **EditMode** (`Game1.cs:7083`) - Editor modes: VertexPlacement, Selection, Delete, SpritePlace, Slope
- **SpriteAlignment** (`Game1.cs:6895`) - Floor, Wall, Ceiling alignment options
- **SpriteTag** (`Game1.cs:6902`) - Sprite categorization system
- **SectorType** (`Game1.cs:6972`) - Different sector behaviors
- **LiftState** (`Game1.cs:7066`) - Lift/elevator states

### Utility Classes
- **VertexHeight** (`Game1.cs:6940`) - Vertex elevation data
- **SlopePlane** (`Game1.cs:6948`) - Slope geometry calculations
- **SectorExtensions** (`Game1.cs:7093`) - Extension methods for sectors
- **TagConstants** (`Game1.cs:131`) - Constant definitions

## Key Features

### Editing Modes
1. **Vertex Placement** - Create and place sector vertices
2. **Selection Mode** - Select and manipulate existing geometry
3. **Delete Mode** - Remove vertices, walls, sectors
4. **Sprite Placement** - Add and position 3D sprites
5. **Slope Mode** - Create sloped floors and ceilings

### 3D Interaction System
- 3D cursor with surface snapping (floor/wall/ceiling)
- 3D sprite manipulation with gizmo system
- Mouse-based 3D positioning and dragging
- Sprite alignment and height management

### UI System
- **Properties Panel** - Object property editing
- **Tools Panel** - Mode selection and tools
- **Status Panel** - Real-time editor information
- **Sprite Editor Window** - Dedicated sprite editing interface

### Data Management
- Sector creation (independent and nested)
- Player position system
- Sprite texture management
- JSON serialization support (via Newtonsoft.Json)

## Build Commands
```bash
dotnet build                    # Build the project
dotnet run                      # Run the application
dotnet restore                  # Restore dependencies
```

## Development Notes

### Performance Considerations
- Main game file (Game1.cs) is 300KB+ containing all logic
- Uses unsafe code blocks (AllowUnsafeBlocks enabled)
- Optimized for desktop GL rendering

### Architecture Patterns
- Monolithic design with single large class containing most functionality
- Event-driven input handling (mouse/keyboard states)
- Component-based sprite and sector management
- Real-time 2D/3D hybrid rendering system

### Key Variables & State
- `_sectors` - List of all sectors in the level
- `_currentEditMode` - Active editing mode
- `_cursor3DPosition` - 3D cursor world position
- `_selectedSprite3D` - Currently selected 3D sprite
- `_playerPosition` - Player spawn location
- `_camera` - 2D camera for viewport control

## Extension Points
- Sprite texture loading system
- Sector behavior customization
- UI panel extensions
- Export/import functionality
- Additional editing modes

This project represents a comprehensive level editor with both 2D editing interface and 3D preview capabilities, suitable for building complex game environments with sectors, walls, and interactive sprites.