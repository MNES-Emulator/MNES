using Mnes.Core.Machine.CPU;

namespace MNES.Core.Machine.Logging;

public readonly struct InstructionLog {
    public readonly CpuInstruction Instruction;
    public readonly ushort Address;
    public readonly byte? D1;
    public readonly byte? D2;
    public readonly CpuRegisterLog CpuRegisters;
    public readonly long ClockCycle;
    public readonly string Message;

    public InstructionLog(CpuInstruction instruction, ushort address, byte? d1, byte? d2, CpuRegisterLog log, long clock_cycle, string message) {
        Instruction = instruction;
        Address = address;
        D1 = d1;
        D2 = d2;
        CpuRegisters = log;
        ClockCycle = clock_cycle;
        Message = message;
    }

    public override string ToString() =>
        $"{Address:X4}  " +
        $"{Instruction.OpCode:X2} {D1?.ToString("X2") ?? ""} {D2?.ToString("X2") ?? ""}".PadRight(10) +
        $"{Instruction.Name} {Message ?? ""}".PadRight(34) +
        $"{CpuRegisters} CYC:{ClockCycle}";
}
