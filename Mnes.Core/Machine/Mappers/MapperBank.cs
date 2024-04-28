namespace Mnes.Core.Machine.Mappers;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class MapperBank {
   public enum AccessorType {
      CPU,
      PPU,
   }

   public enum BankType {
      PRG_RAM,
      ROM,
      CHR,
   }

   public AccessorType Accessor { get; init; }
   public BankType Type { get; init; }
   public Range AccessRange { get; init; }
   public Range DestinationRange { get; init; }
   public ushort MemorySize { get; init; }

   public MapperBank(
      AccessorType accessor,
      BankType type,
      Range access_range,
      Range destination_range
   ) {
      Accessor = accessor;
      Type = type;
      AccessRange = access_range;
      DestinationRange = destination_range;
   }
}
