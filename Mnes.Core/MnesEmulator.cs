using Emu.Core;
using Mnes.Core.Machine;
using Mnes.Core.Machine.Input;
using Mnes.Core.Saves.Configuration;

namespace Mnes.Core;

public sealed class MnesEmulator : Emulator {
   readonly MachineState _machine;

   public override bool IsRunning => throw new NotImplementedException();

   public MnesEmulator(string rom_path, MnesConfig config, NesInputState input) : base(rom_path, config, input) {
      _machine = new(File.ReadAllBytes(rom_path), config, input);

   public override EmulatorScreen Screen => _machine.Ppu.Screen;

   public override void Run() =>
      _ = _machine.Run();

   public override void Stop() {
   }
}
