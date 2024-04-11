namespace Emu.Core;

public abstract class Emulator {
   public readonly string RomPath;
   public readonly EmulatorConfig Config;
   public readonly InputState InputState;

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
