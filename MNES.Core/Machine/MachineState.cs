using Mnes.Core.Machine.CPU;
using Mnes.Core.Machine.Input;
using Mnes.Core.Machine.Mappers;
using Mnes.Core.Saves.Configuration;
using Mnes.Core.Machine.Logging;
using Mnes.Core.Machine.PPU;
using Mnes.Core.Machine.IO;

namespace Mnes.Core.Machine;

public sealed class MachineState {
   readonly InesHeader header;
   readonly Mapper mapper;
   readonly NesTimer timer;
   readonly InputState input;
   byte last_read_value; // returns in case of open bus reads

   public readonly byte[] Ram = new byte[0x0800];
   public readonly byte[] Rom;
   public readonly Ppu Ppu = new();
   public readonly Apu Apu = new();
   public readonly Cpu Cpu;
   public readonly IoRegisters Io;
   public readonly MachineLogger Logger;
   public readonly ConfigSettings Settings;

   public MachineState(
      string rom_path,
      ConfigSettings settings,
      InputState input
   ) {
      Settings = settings;
      this.input = input;

      var nes_bytes = File.ReadAllBytes(rom_path);
      header = new InesHeader(nes_bytes);
      Rom = nes_bytes[InesHeader.header_length..];

      mapper = Mapper.GetMapperOrThrow(header, this);

      Cpu = new(this);
      timer = new(settings.System.Region, settings.System.DebugMode ? DebugTick : Tick);
      Logger = new(this);
      Io = new(this);
   }

   public async Task Run() {
      SetPowerUpState();
      Cpu.Registers.PC = ReadUShort(0xFFFC);
      timer.Start();
      await timer.RunningThread;
   }

   void DebugTick() {
      Cpu.Tick();
      Ppu.Tick();
      Ppu.Tick();
      Ppu.Tick();
   }


   void Tick() {
      Cpu.Tick();
      Ppu.Tick();
      Ppu.Tick();
      Ppu.Tick();
   }

   void SetPowerUpState() {
      Cpu.SetPowerUpState();
      Ppu.SetPowerUpState();

      for (int i = 0; i < Ram.Length; i++)
         Ram[i] = 0x00;
   }

   public ushort ReadUShort(int index) =>
      ReadUShort((ushort)index);

   public ushort ReadUShort(ushort index) {
      var b_l = this[index];
      ushort b_h = this[(ushort)(index + 1)];
      b_h <<= 8;
      b_h |= b_l;
      return b_h;
   }

   public ushort ReadUShortSamePage(
      ushort index
   ) {
      var b_l = this[index];

      // force it to stay on the same page
      var b_h_address = (ushort)(index + 1);
      b_h_address &= 0b_0000_0000_1111_1111;
      b_h_address |= (ushort)(0b_1111_1111_0000_0000 & index);

      ushort b_h = this[b_h_address];
      b_h <<= 8;
      b_h |= b_l;
      return b_h;
   }

   /// <summary> If reads null, then open bus read. Don't write null. </summary>
   public byte this[ushort index] { get {
      last_read_value =
          index < 0x2000 ? Ram[index % 0x0800] :
          index < 0x4000 ? Ppu.Registers[index % 8] :
          index < 0x4020 ? Io[index - 0x4020] ?? last_read_value :
          mapper[index] ?? last_read_value;
      return last_read_value;
   } set {
      if (index < 0x2000) Ram[index % 0x0800] = value;
      else if (index < 0x4000) Ppu.Registers[index % 8] = value;
      else if (index < 0x4020) Io[index - 0x4020] = value;
      else mapper[index] = value;
   } }
}
