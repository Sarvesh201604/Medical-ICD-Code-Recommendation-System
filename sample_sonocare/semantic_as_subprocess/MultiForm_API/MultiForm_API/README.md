# Sonocare Application (.NET/WPF)

## 1. Project Overview
Sonocare is a desktop application designed for medical reporting. It allows users to manage patient records, generate reports, and input data using voice commands.

**Current Version:** 4.0 (Migrated to .NET 8)
**Platform:** Windows (x64)

## 2. Technology Stack

### Backend Logic
*   **Language:** C#
*   **Framework:** .NET 8.0 (Modern, high-performance cross-platform framework)
*   **Database:** SQLite (Lightweight, serverless relational database)
*   **ORM:** Entity Framework Core (Handles database interactions using C# objects instead of raw SQL)

### Frontend (UI)
*   **Framework:** WPF (Windows Presentation Foundation)
*   **Language:** XAML (Extensible Application Markup Language) for UI layout
*   **Styling:** Native WPF Styles & Templates

### Voice Integration (Augnito)
*   **Engine:** WebView2 (Microsoft Edge Rendering Engine)
*   **Integration:** A "Headless" (Invisible) Browser approach.
*   **Mechanism:**
    1.  The `.NET` app launches a hidden `WebView2` control.
    2.  This control loads `augnito_headless.html`.
    3.  The HTML loads the Augnito JavaScript SDK.
    4.  C# communicates with the JavaScript SDK to receive voice text and command focus.

## 3. Architecture & File Structure

The project follows a standard **MVVM-like** structure (Model-View-ViewModel), though currently implemented with Code-Behind for simplicity.

### Root Directory (`SonocareApp/`)
*   **`SonocareApp.sln`**: The Visual Studio Solution file. Open this to work on the project.
*   **`SonocareApp.csproj`**: Defines project settings and dependencies (`EntityFrameworkCore`, `WebView2`).
*   **`App.xaml`**: The entry point of the application. It handles startup logic.
*   **`augnito_headless.html`**: The bridge for voice commands. It contains the JavaScript logic to connect to the Augnito Voice Server.

### ➤ /Views (The User Interface)
These files define what the user sees. Each has a `.xaml` (design) and `.xaml.cs` (logic) file.
*   **`MainWindow.xaml`**: The Dashboard. It lists all patients and provides search/filter options.
*   **`AddPatientWindow.xaml`**: A dialog window to register a new patient.
*   **`ReportWindow.xaml`**: The main reporting interface. It contains the form fields (BPD, HC, etc.) and handles the Voice Command logic bridge.
*   **`HistoryWindow.xaml`**: Displays past reports for a selected patient.

### ➤ /Models (Data Structure)
These classes define the "shape" of the data stored in the database.
*   **`Patient.cs`**: Represents a patient entity (Name, Age, ID, etc.).
*   **`Report.cs`**: Represents a medical report entity (BPD, HC, AC, FL values, Visit Date).

### ➤ /Data (Database Layer)
*   **`AppDbContext.cs`**: The "Database Context". It manages the connection to `database.db` and translates C# `Patient` and `Report` objects into SQL database rows.

## 4. How It Works

### Frontend Construction (WPF)
The UI is built using **XAML**. Buttons, TextBoxes, and Grids are defined in XML-like tags.
*   **Styling**: The look and feel are native Windows controls.
*   **Events**: User actions (clicks, typing) are handled in the `.xaml.cs` files (e.g., `Button_Click`).

### Backend Construction (C# & EF Core)
*   **Data Access**: When you save a patient, `AppDbContext` takes the C# object and saves it to `database.db`.
*   **Logic**: C# handles the navigation between windows (e.g., clicking a patient in `MainWindow` opens `ReportWindow`).

### Voice Construction (WebView2 Bridge)
*   The `ReportWindow.xaml.cs` initializes a `WebView2` control.
*   It injects a C# object (`Bridge`) into the Javascript environment.
*   When Augnito hears a word, Javascript calls `window.chrome.webview.postMessage(text)`.
*   C# receives this message and decides which TextBox to focus or what text to type.
