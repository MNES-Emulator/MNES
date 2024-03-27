using Mnes.Core.Machine.CPU;
using Mnes.Core.Machine.Input;
using Mnes.Core.Machine.Mappers;
using Mnes.Core.Saves.Configuration;
using MNES.Core.Machine.Logging;

namespace Mnes.Core.Machine;

public sealed class MachineState
{
    readonly INesHeader header;
    readonly Mapper mapper;
    readonly NesTimer timer;
    readonly InputState input;
    byte last_read_value = 0; // returns in case of open bus reads

    public readonly byte[] Ram = new byte[0x0800];
    public readonly byte[] Rom;
    public readonly Ppu Ppu = new();
    public readonly Apu Apu = new();
    public readonly Cpu Cpu;
    public readonly MachineLogger Logger;
    public readonly ConfigSettings Settings;

    public MachineState(string rom_path, ConfigSettings settings, InputState input)
    {
        this.Settings = settings;
        this.input = input;
        var nes_bytes = File.ReadAllBytes(rom_path);
        header = new INesHeader(nes_bytes);
        Rom = new byte[nes_bytes.Length - 16];
        nes_bytes[16..].CopyTo(Rom, 0);
        mapper = Mapper.GetMapper(header, this);
        if (mapper == null) throw new NotImplementedException($"Mapper {header.MapperNumber} is not implemented.");
        Cpu = new(this);
        timer = new(settings.System.Region, settings.System.DebugMode ? DebugTick : Cpu.Tick);
        Logger = new(this);
    }

    public async Task Run()
    {
        SetPowerUpState();
        //Cpu.Registers.PC = (ushort)(this[0xFFFC] + (this[0xFFFD] << 8));
        var r = ReadUShort(0xFFFC);

        Cpu.Registers.PC = 0xC000;
        timer.Start();
        await timer.RunningThread;
    }

    void DebugTick()
    {
        Cpu.Tick();
    }

    void SetPowerUpState()
    {
        Cpu.SetPowerUpState();
        Ppu.SetPowerUpState();
        Apu.SetPowerUpState();
        for (int i = 0; i < Ram.Length; i++) Ram[i] = 0x00;
    }

    public ushort ReadUShort(int index) => ReadUShort((ushort)index);
    public ushort ReadUShort(ushort index)
    {
        var b_l = this[index];
        ushort b_h = this[(ushort)(index + 1)];
        b_h <<= 8;
        b_h |= b_l;
        return b_h;
    }

    /// <summary> If reads null, then open bus read. Don't write null. </summary>
    public byte this[ushort index] {
        get {
            last_read_value =
                index < 0x2000 ? Ram[index % 0x0800] :
                index < 0x4000 ? Ppu.Registers[index % 8] :
                index < 0x4020 ? Apu.Registers[index - 0x4000] :
                mapper[index] ?? last_read_value;
            return last_read_value;
        }
        set
        {
            if (index < 0x2000) Ram[index % 0x0800] = value;
            else if (index < 0x4000) Ppu.Registers[index % 8] = value;
            else if (index < 0x4020) Apu.Registers[index - 0x4000] = value;
            else mapper[index] = value;
        }
    }
}
