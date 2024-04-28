using Mnes.Core.Machine.Mappers.Implementation;

namespace Mnes.Core.Machine.Mappers;

// https://www.nesdev.org/wiki/Mapper
public abstract class Mapper {
   protected readonly MachineState Machine;
   public abstract int MapperNumber { get; }

   protected abstract MapperBank[] Banks { get; }

   /// <summary> If reads null, then open bus read. Don't write null. </summary>
   public abstract byte? this[ushort index] { get; set; }

   public virtual void ProcessCycle(int cycle, int scanline) { }

   public Mapper(
      InesHeader header,
      MachineState machine
   ) =>
      Machine = machine;

   public static Mapper? MaybeGetMapper(
      InesHeader header,
      MachineState machine
   ) =>
      header.MapperNumber == 0
         ? new M000(header, machine)
         : null;

   public static Mapper GetMapperOrThrow(
      InesHeader header,
      MachineState machine
   ) =>
      MaybeGetMapper(header, machine)
      ?? throw new NotImplementedException($"Mapper {header.MapperNumber} is not implemented.");
}
