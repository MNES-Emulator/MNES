namespace Emu.Core;

public abstract class Emulator : IDisposable {
   public string RomPath { get; }
   public EmulatorConfig Config { get; }
   public InputState InputState { get; }

   public abstract EmulatorScreen Screen { get; }

   public Emulator(string rom_path, EmulatorConfig config, InputState input) {
      RomPath = rom_path;
      Config = config;
      InputState = input;
   }

   public abstract void Run();
   public abstract void Stop();
   public virtual void Dispose() { }
}
