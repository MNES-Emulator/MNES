using Mnes.Core.Machine.CPU;
using Mnes.Core.Machine.Input;
using Mnes.Core.Machine.Mappers;
using Mnes.Core.Saves.Configuration;
using Mnes.Core.Machine.Logging;
using Mnes.Core.Machine.PPU;
using Mnes.Core.Machine.IO;

namespace Mnes.Core.Machine;

public sealed class MachineState {
   readonly InesHeader _header;
   readonly Mapper _mapper;
   readonly NesTimer _timer;
   readonly InputState _input;
   byte _last_read_value; // returns in case of open bus reads

   public byte[] Ram { get; } = new byte[0x0800];
   public byte[] Rom { get; }
   public Ppu Ppu { get; }
   public Apu Apu { get; } = new();
   public Cpu Cpu { get; }
   public IoRegisters Io { get; }
   public MachineLogger Logger { get; }
   public ConfigSettings Settings { get; }

   public MachineState(
      byte[] nes_bytes,
      ConfigSettings settings,
      InputState input
   ) {
      Settings = settings;
      _input = input;

      _header = new InesHeader(nes_bytes);
      Rom = nes_bytes[InesHeader.header_length..];

      _mapper = Mapper.GetMapperOrThrow(_header, this);

      Cpu = new(this);
      _timer = new(settings.System.Region, settings.System.DebugMode ? DebugTick : Tick);
      Logger = new(this);
      Io = new(this);
      Ppu = new(this);
   }

   public async Task Run() {
      SetPowerUpState();
      Cpu.Registers.PC = ReadUShort(0xFFFC);
      _timer.Start();
      await _timer.RunningThread;
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
      _last_read_value =
          index < 0x2000 ? Ram[index % 0x0800] :
          index < 0x4000 ? Ppu.Registers[index % 8] :
          index < 0x4020 ? Io[index - 0x4020] ?? _last_read_value :
          _mapper[index] ?? _last_read_value;
      return _last_read_value;
   } set {
      if (index < 0x2000) Ram[index % 0x0800] = value;
      else if (index < 0x4000) Ppu.Registers[index % 8] = value;
      else if (index < 0x4020) Io[index - 0x4020] = value;
      else _mapper[index] = value;
   } }
}
