## Godot UI
Before opening the [Godot](https://godotengine.org/) project, make sure you are using 4.2.1. You will also need to use the C# version (separate download).

To debug the Godot GUI from your IDE, add the following settings to a launch profile for `Mnes.Ui.Godot`.

 - Executable: `(path to your Godot exe)`
 - Command line arguments: `--path . --verbose "../Resources/Test Roms/other/nestest.nes"`
 - Working directory: `.`

You can launch without launching a ROM by removing the path to the .nes file in the command line arguments. You can also launch and build directly from Godot after opening the project.

![Settings](Resources/Images/Setup%20MNES%20Godot.png)
