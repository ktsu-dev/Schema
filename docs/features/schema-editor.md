# Schema Editor

The Schema Editor is a visual desktop application for creating and modifying schemas using an intuitive graphical interface built with ImGui.

## Overview

The Schema Editor provides a user-friendly way to:

-   Create and edit schema definitions visually
-   Navigate complex schemas with a tree-based interface
-   Edit properties and types interactively
-   Validate schemas in real-time
-   Import/export schema files

## Features

### 🎨 **Visual Interface**

-   Tree view for navigating schema hierarchy
-   Property panels for editing details
-   Type selection dropdowns
-   Context menus for quick actions
-   Drag-and-drop support

### 🔧 **Schema Management**

-   Create new schemas from scratch
-   Open existing `.schema.json` files
-   Save schemas with validation
-   Import from various formats
-   Export to different targets

### ⚡ **Real-time Validation**

-   Instant feedback on schema validity
-   Error highlighting and messages
-   Type compatibility checking
-   Circular reference detection

### 📊 **Advanced Editing**

-   Multi-selection operations
-   Bulk property changes
-   Search and filter capabilities
-   Undo/redo support

## Getting Started

### Installation

1. Build the SchemaEditor project:

    ```bash
    dotnet build SchemaEditor/SchemaEditor.csproj
    ```

2. Run the application:
    ```bash
    dotnet run --project SchemaEditor
    ```

### First Launch

When you first launch the Schema Editor:

1. **Welcome Screen**: Choose to create a new schema or open an existing one
2. **New Schema**: Start with a blank schema and begin adding elements
3. **Open Schema**: Browse and select an existing `.schema.json` file

## User Interface

### Main Window Layout

```
┌─────────────────────────────────────────────────────────────┐
│ File  Edit  View  Schema  Tools  Help                      │
├─────────────────┬───────────────────────────────────────────┤
│ Schema Tree     │ Property Panel                            │
│                 │                                           │
│ ├─ Classes      │ ┌─ Selected Item Properties ─┐           │
│ │  ├─ User      │ │ Name: [User            ]    │           │
│ │  └─ Project   │ │ Type: Class                 │           │
│ ├─ Enums        │ │                             │           │
│ │  └─ Status    │ │ Members:                    │           │
│ ├─ DataSources  │ │ ├─ Id (Int)                 │           │
│ └─ Generators   │ │ ├─ Name (String)            │           │
│                 │ │ └─ Email (String)           │           │
│                 │ └─────────────────────────────┘           │
├─────────────────┴───────────────────────────────────────────┤
│ Status: Schema loaded successfully │ Validation: ✓ Valid   │
└─────────────────────────────────────────────────────────────┘
```

### Tree View

The left panel shows a hierarchical view of the schema:

#### 📁 **Classes**

-   Expandable list of all schema classes
-   Show member count in parentheses
-   Right-click for context menu (Add Member, Delete, Rename)

#### 📁 **Enums**

-   List of all enumerations
-   Show value count in parentheses
-   Right-click for context menu (Add Value, Delete, Rename)

#### 📁 **Data Sources**

-   Configured data sources
-   Show associated class and file
-   Right-click for context menu (Edit, Delete)

#### 📁 **Code Generators**

-   Code generation configurations
-   Show target language and output path

### Property Panel

The right panel shows details for the selected item:

#### Class Properties

```
Name: [User                    ]
Description: [User account     ]

Members:
┌─────────────────────────────────────┐
│ + Add Member                        │
│ Id        │ Int     │ [Edit] [Del]  │
│ Name      │ String  │ [Edit] [Del]  │
│ Email     │ String  │ [Edit] [Del]  │
│ Role      │ Enum    │ [Edit] [Del]  │
└─────────────────────────────────────┘
```

#### Member Properties

```
Name: [Email                   ]
Type: [String          ▼]
Description: [User's email     ]
Required: [✓]
Default: [                     ]
```

#### Enum Properties

```
Name: [UserRole               ]
Description: [User role types ]

Values:
┌─────────────────────────────────────┐
│ + Add Value                         │
│ Admin     │ [Edit] [Del]            │
│ User      │ [Edit] [Del]            │
│ Guest     │ [Edit] [Del]            │
└─────────────────────────────────────┘
```

## Working with Schemas

### Creating a New Schema

1. **File → New Schema** or use Ctrl+N
2. Choose a location and filename
3. Start adding classes and enums
4. Configure data sources if needed
5. Save with Ctrl+S

### Adding Elements

#### Adding a Class

1. Right-click on "Classes" in the tree
2. Select "Add Class"
3. Enter the class name
4. Add members to the class

#### Adding Members

1. Select a class in the tree
2. Click "+ Add Member" in the property panel
3. Set the member name and type
4. Configure additional properties

#### Adding Enums

1. Right-click on "Enums" in the tree
2. Select "Add Enum"
3. Enter the enum name
4. Add values to the enum

### Type Selection

When setting member types, you can choose from:

-   **Primitives**: `int`, `long`, `float`, `double`, `string`, `bool`, `DateTime`, `TimeSpan`
-   **Vectors**: `Vector2`, `Vector3`, `Vector4`, `ColorRGB`, `ColorRGBA`
-   **Arrays**: Select element type and container
-   **Objects**: Reference to schema classes
-   **Enums**: Reference to schema enums

### Array Configuration

When selecting an Array type:

1. Choose the element type (primitive, class, or enum)
2. Select container type:
    - `vector` - Simple array/list
    - `map` - Key-value collection
3. For maps, specify the key member

### Data Source Configuration

To add a data source:

1. Right-click on "Data Sources"
2. Select "Add Data Source"
3. Configure:
    - **Name**: Identifier for the data source
    - **File**: Relative path to data file
    - **Class**: Associated schema class
    - **Format**: JSON, XML, etc.

## Advanced Features

### Schema Validation

The editor continuously validates the schema:

-   **Type Consistency**: Ensures all type references are valid
-   **Circular References**: Detects and warns about circular dependencies
-   **Naming Conflicts**: Prevents duplicate names
-   **Required Fields**: Validates that required properties are set

### Search and Filter

Use the search box to find elements:

-   Search by name across all schema elements
-   Filter by type (classes, enums, etc.)
-   Navigate to search results quickly

### Import/Export

#### Supported Import Formats

-   `.schema.json` files
-   JSON Schema files (partial support)
-   C# class definitions (experimental)

#### Export Options

-   Standard `.schema.json` format
-   JSON Schema format
-   Documentation (Markdown)
-   Code generation templates

### Keyboard Shortcuts

| Action          | Shortcut     |
| --------------- | ------------ |
| New Schema      | Ctrl+N       |
| Open Schema     | Ctrl+O       |
| Save Schema     | Ctrl+S       |
| Save As         | Ctrl+Shift+S |
| Add Class       | Ctrl+Shift+C |
| Add Enum        | Ctrl+Shift+E |
| Delete Selected | Del          |
| Rename Selected | F2           |
| Find            | Ctrl+F       |
| Undo            | Ctrl+Z       |
| Redo            | Ctrl+Y       |

## Configuration

### Application Settings

Access via **Tools → Options**:

#### General

-   Auto-save interval
-   Recent files count
-   Default schema location

#### Editor

-   Tree expansion behavior
-   Property panel layout
-   Validation settings

#### Appearance

-   Theme selection (Light/Dark)
-   Font size and family
-   Color scheme

### Project Settings

Per-schema settings stored in `.schema.json`:

```json
{
    "EditorSettings": {
        "TreeExpansion": ["Classes", "Enums"],
        "LastSelectedItem": "Classes/User",
        "WindowLayout": "Standard"
    }
}
```

## Troubleshooting

### Common Issues

#### Schema Won't Load

-   **Check file format**: Ensure it's valid JSON
-   **Verify file path**: Confirm the file exists and is accessible
-   **Check permissions**: Ensure read access to the file

#### Validation Errors

-   **Missing references**: Ensure all type references point to existing classes/enums
-   **Circular dependencies**: Remove circular references between classes
-   **Invalid names**: Use valid identifiers for names

#### Performance Issues

-   **Large schemas**: Consider breaking into multiple files
-   **Deep nesting**: Flatten overly complex hierarchies
-   **Memory usage**: Close unused schema files

### Error Messages

| Error                | Cause                            | Solution                                   |
| -------------------- | -------------------------------- | ------------------------------------------ |
| "Class not found"    | Invalid class reference          | Update type reference or add missing class |
| "Circular reference" | Classes reference each other     | Break the circular dependency              |
| "Invalid name"       | Name contains invalid characters | Use valid identifier names                 |
| "File not found"     | Data source file missing         | Update file path or create missing file    |

## Best Practices

### Schema Organization

-   Group related classes together
-   Use clear, descriptive names
-   Document complex types
-   Keep hierarchies manageable

### Performance Tips

-   Save frequently to avoid data loss
-   Use validation before saving
-   Close unused schemas
-   Regular cleanup of temporary files

### Collaboration

-   Use version control for schema files
-   Document schema changes
-   Establish naming conventions
-   Regular schema reviews

## See Also

-   [Getting Started](../getting-started.md) - Basic schema creation
-   [Schema Definition](schema-definition.md) - Programmatic schema creation
-   [Type System](type-system.md) - Available types and their usage
-   [Code Generation](code-generation.md) - Generating code from schemas
