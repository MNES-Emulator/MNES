using Mnes.Core.Machine.Mappers.Implementation;

namespace Mnes.Core.Machine.Mappers;

// https://www.nesdev.org/wiki/Mapper
public abstract class Mapper
{
    protected readonly MachineState Machine;
    public abstract int MapperNumber { get; }

    protected abstract MapperBank[] Banks { get; }

    public Mapper(INesHeader header, MachineState machine)
    {
        Machine = machine;
    }

    /// <summary> If reads null, then open bus read. Don't write null. </summary>
    public abstract byte? this[ushort index] { get; set; }

    public static Mapper GetMapper(INesHeader header, MachineState machine) =>
        header.MapperNumber == 0 ? new M000(header, machine) :
        null;
}
