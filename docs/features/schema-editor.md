# Schema Editor

The Schema Editor is a visual desktop application for creating and modifying schemas using an intuitive graphical interface built with ImGui.

## Overview

The Schema Editor provides a user-friendly way to:

- Create and edit schema definitions visually
- Navigate schemas with a tree-based interface
- Edit member properties and types interactively
- Save and load `.schema.json` files

## Getting Started

### Building and Running

1. Build the SchemaEditor project:

    ```bash
    dotnet build SchemaEditor/SchemaEditor.csproj
    ```

2. Run the application:
    ```bash
    dotnet run --project SchemaEditor
    ```

### First Launch

When you first launch the Schema Editor, use the **File** menu to create a new schema or open an existing `.schema.json` file. The editor remembers your last opened file and restores it on next launch.

## User Interface

### Main Window Layout

```
┌─────────────────────────────────────────────────────────────┐
│ File                                                         │
├─────────────────┬───────────────────────────────────────────┤
│ Schema Tree     │ Property Panel                            │
│                 │                                           │
│ ├─ Classes      │ Schema file path                          │
│ │  ├─ User (3)  │                                           │
│ │  └─ Project(2)│ Members:                                  │
│ ├─ Enums        │ ┌───────────────────────────────────────┐ │
│ │  └─ Status(3) │ │ Name  │ Type   │ Container │ Key     │ │
│ ├─ DataSources  │ │ Id    │ Int    │           │         │ │
│ └─ CodeGens     │ │ Name  │ String │           │         │ │
│                 │ │ Email │ String │           │         │ │
│                 │ └───────────────────────────────────────┘ │
│                 │ [+ New Member]                             │
└─────────────────┴───────────────────────────────────────────┘
```

The layout is split into two resizable panels. Panel sizes are persisted across sessions.

### File Menu

- **New** - Creates a new empty schema
- **Open** - Opens an existing `.schema.json` file via file browser
- **Save** - Saves the current schema (prompts for location if unsaved)
- **Open Externally** - Opens the schema file's directory in the system file explorer

### Left Panel - Schema Tree

The left panel shows four collapsible tree sections:

**Classes** - Expandable list showing each class with its member count. Click a class to edit it in the right panel. Each class expands to show its members. Right-click to delete.

**Enums** - List of enumerations with value count. Each enum expands to show its values. Right-click to delete entries.

**Data Sources** - List of configured data sources. Click to edit in the right panel. Right-click to delete.

**Code Generators** - List of code generator configurations. Right-click to delete.

Each section has a button to add new items via a popup dialog.

### Right Panel - Property Editor

The right panel content depends on what's selected:

**When a Class is selected:**

- Displays the schema file path
- Shows a table of all members with columns: Name, Type, Container, Key
- Each member has a delete button and type picker
- Array types show additional container name and key selector fields
- "New Member" button at the bottom

**When a Data Source is selected:**

- File path input field
- Class selector dropdown

**When nothing is selected:**

- Shows the schema file path (or a warning if the schema hasn't been saved yet)

### Type Selection

When setting member types via the type picker, you can choose from:

- **Primitives**: `Int`, `Long`, `Float`, `Double`, `String`, `Bool`, `DateTime`, `TimeSpan`
- **Vectors**: `Vector2`, `Vector3`, `Vector4`, `ColorRGB`, `ColorRGBA`
- **Arrays**: Select element type and container name
- **Objects**: Reference to schema classes
- **Enums**: Reference to schema enums

The type selector popup includes a searchable list of available types.

## Session Persistence

The editor automatically persists:

- Last opened schema file path
- Last selected class
- Panel divider positions
- Tree node expand/collapse state

These settings are stored via `ktsu.AppDataStorage` and restored on next launch.

## See Also

- [Getting Started](../getting-started.md) - Basic schema creation with the library API
- [Basic Schema Example](../examples/basic-schema.md) - Complete walkthrough
