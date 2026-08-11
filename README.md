# DfTools

DfTools is a keyboard-first developer utilities desktop application written in C# (.NET). It is inspired by [DevToolbox](https://github.com/aleiepure/devtoolbox), tailored for quick keyboard-driven workflows.

This project is a personal exploration aimed at validating development and tests techniques while building a usefull tool intended for daily use.

Df stands for Danilo Florenzano (very creative, don't you think?)

<img width="1000" height="700" alt="image" src="https://github.com/user-attachments/assets/d5899edc-fbdf-4e1e-b66a-7dc3359dfd7b" />

## Features

- **Keyboard-First Interface**: Streamlined navigation and actions optimized for keyboard productivity without requiring mouse interaction.
- **SQL Query Formatter**: Formats and beautifies complex SQL queries cleanly. Powered by a C# port of [doctrine/sql-formatter](https://github.com/doctrine/sql-formatter).
- **Text Diff**: Compares two text snippets line-by-line and character-by-character using side-by-side visualization. Powered by [DiffPlex](https://github.com/mmanela/diffplex).

## Installation & Running

Download the pre-built zip for your operating system from the [Latest Release](https://github.com/daniloflorenzano/dftools/releases/latest).

### Linux

1. Download `dftools-v0.2.0-linux-x64.zip`.
2. Extract the archive:
   ```bash
   unzip dftools-v0.2.0-linux-x64.zip -d dftools
   cd dftools
   ```
3. Make executable (if needed) and run:
   ```bash
   chmod +x DfTools
   ./DfTools
   ```

### Windows

1. Download `dftools-v0.2.0-win-x64.zip`.
2. Extract the zip folder.
3. Double-click `DfTools.exe` (or run `.\DfTools.exe` in PowerShell / Command Prompt).


## Project Goals

- Provide an extensible framework for developer utilities in .NET.
- Validate desktop app UI/UX patterns for quick-access developer tooling.

## Credits & Inspiration

- Inspired by [DevToolbox](https://github.com/aleiepure/devtoolbox) by [aleiepure](https://github.com/aleiepure).
- SQL formatting logic ported from [doctrine/sql-formatter](https://github.com/doctrine/sql-formatter).
- Text diff engine powered by [DiffPlex](https://github.com/mmanela/diffplex) by [mmanela](https://github.com/mmanela).

## License

MIT License.

