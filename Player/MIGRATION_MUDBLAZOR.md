# Migration to MudBlazor

This document describes the migration from FluentUI to MudBlazor in the MySpeaker project.

## Changes Made

### 1. Package Reference (MySpeaker.csproj)
- **Removed**: `Microsoft.FluentUI.AspNetCore.Components` v4.13.1
- **Added**: `MudBlazor` v8.0.0

### 2. Service Registration (Program.cs)
- **Changed**: `builder.Services.AddFluentUIComponents()` ? `builder.Services.AddMudServices()`
- **Updated**: Using directive from `Microsoft.FluentUI.AspNetCore.Components` ? `MudBlazor.Services`

### 3. App Root Component (Components/App.razor)
- **Replaced FluentUI providers** with MudBlazor equivalents:
  - `FluentDesignSystemProvider` ? `MudThemeProvider`
  - `FluentDesignTheme` ? Custom `MudTheme` configuration
- **Added MudBlazor providers**:
  - `MudPopoverProvider`
  - `MudDialogProvider`
  - `MudSnackbarProvider`
- **Updated CSS references**:
  - Removed FluentUI CSS files
  - Added MudBlazor CSS and Google Fonts (Roboto)
- **Added custom dark theme** with matching color scheme:
  - Primary: #74c2ff (light blue)
  - Secondary: #b084ff (purple)
  - Background: #080b12 (dark)
  - Surface: #1a1f2e

### 4. Global Imports (Components/_Imports.razor)
- **Changed**: `@using Microsoft.FluentUI.AspNetCore.Components` ? `@using MudBlazor`

### 5. Layout Component (Components/Layout/MainLayout.razor)
- **Replaced**:
  - `FluentStack` ? `MudLayout`, `MudAppBar`, `MudMainContent`, `MudStack`
  - FluentUI layout structure ? MudBlazor's standard app layout
- **Component mappings**:
  - Header section ? `MudAppBar` with custom class
  - Main content ? `MudMainContent`
  - Text elements ? `MudText` with appropriate `Typo` settings

### 6. Home Page (Components/Pages/Home.razor)
Extensive component replacements throughout:

- **Layout Components**:
  - `FluentStack` ? `MudStack` (with `Row`, `Spacing`, `Justify`, `AlignItems` props)
  - `FluentCard` ? `MudPaper` (with `Elevation` and `Class` props)
  - `FluentTabs`/`FluentTab` ? `MudTabs`/`MudTabPanel`

- **Form Components**:
  - `FluentButton` ? `MudButton` (with `Variant`, `Color`, `Size`, `ButtonType` props)
  - `FluentTextField` ? `MudTextField` (with generic type `T="string"`)
  - `FluentTextArea` ? `MudTextField` with `Lines` prop
  - `FluentSelect`/`FluentOption` ? `MudSelect`/`MudSelectItem` (with generic type `T="string"`)
  - `FluentSlider` ? `MudSlider` (with generic type `T="int"`)

- **Display Components**:
  - `FluentBadge` ? `MudChip` (with generic type `T="string"`)
  - `FluentMessageBar` ? `MudAlert` (with `Severity`, `Variant`, `CloseIcon` props)
  - Text elements ? `MudText` with `Typo` variants

- **Container Component**:
  - Root `FluentStack` ? `MudContainer` with `MaxWidth.Large`

### 7. Code-Behind (Components/Pages/Home.razor.cs)
- **Updated method signatures** to accept value parameters:
  - `OnVolumeChangedAsync()` ? `OnVolumeChangedAsync(int value)`
  - `OnSourceChangedAsync()` ? `OnSourceChangedAsync(string value)`
  - `OnLoopChangedAsync()` ? `OnLoopChangedAsync(string value)`
- These changes were necessary because MudBlazor requires explicit `ValueChanged` handlers when not using two-way binding with `@bind-Value`

### 8. CSS Updates
- **fluent-dashboard.css**: Added MudBlazor-specific overrides for better theme integration
- **MainLayout.razor.css**: Updated to work with MudBlazor's AppBar component

## Key Differences Between FluentUI and MudBlazor

### Component API
- **Generic Types**: MudBlazor components often require explicit generic type parameters (e.g., `T="string"`, `T="int"`)
- **Prop Names**: Different property naming conventions:
  - `Appearance` (FluentUI) ? `Variant` (MudBlazor)
  - `Orientation` ? `Row` attribute for horizontal layout
  - `VerticalGap`/`HorizontalGap` ? `Spacing` with numeric value

### Layout System
- **MudBlazor** uses a more structured layout with `MudLayout`, `MudAppBar`, and `MudMainContent`
- **FluentUI** used simpler stack-based layouts

### Theming
- **MudBlazor** has built-in dark mode support via `IsDarkMode` prop
- Custom themes are more structured with `PaletteDark`/`PaletteLight`

### Event Handling
- MudBlazor's two-way binding (`@bind-Value`) conflicts with explicit `ValueChanged` handlers
- Solution: Use one-way binding (`Value` + `ValueChanged`) when custom change handlers are needed

## Testing Recommendations

1. **Visual Testing**: Compare the UI appearance before and after migration
2. **Interaction Testing**: Test all buttons, forms, and interactive elements
3. **Responsive Testing**: Verify the layout works on different screen sizes
4. **Theme Testing**: Confirm dark mode is applied correctly throughout

## Benefits of MudBlazor

1. **Active Development**: MudBlazor is actively maintained with frequent updates
2. **Rich Component Library**: More components and variations available
3. **Better Documentation**: Comprehensive docs and examples
4. **Material Design**: Based on Google's Material Design system
5. **Performance**: Generally good performance characteristics
6. **Customization**: Extensive theming and styling options

## Migration Complete ?

The project successfully builds and all FluentUI dependencies have been replaced with MudBlazor equivalents.
