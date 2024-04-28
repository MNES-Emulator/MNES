namespace Mnes.Core.Machine.Mappers.Implementation;

sealed class M000 : Mapper {
   readonly byte[]? prg_ram;
   readonly bool mirror_rom_8000_BFFF;

   public override int MapperNumber => 0;

   // Just ignoring this for now
   protected override MapperBank[] Banks { get; } = {
      //new(MapperBank.AccessorType.CPU, MapperBank.BankType.PRG_RAM, new Range(0x6000, 0x8000), new(0x0000, 0x2000))
   };

   public override byte? this[ushort i] { get {
      if (i >= 0x6000 && i < 0x8000) return prg_ram?[i - 0x6000];
      if (i >= 0x8000 && i < 0xC000) return Machine.Rom[i - 0x8000];

      if (i >= 0xC000)
         return mirror_rom_8000_BFFF
            ? Machine.Rom[(i - 0xC000) % 0x4000]
            : Machine.Rom[i - 0xC000];

      return null;
   } set {
      if (prg_ram != null && i - 0x6000 < prg_ram.Length)
         prg_ram[i - 0x6000] = value ?? 0; // TODO: used null-coalesce to avoid NRE but should this property really be nullable?
   } }

   public M000(
      InesHeader header,
      MachineState machine
   ) : base(header, machine) {
      if (header.PrgRam_6000_7FFF_Present)
         prg_ram = new byte[header.PrgRamSize];

      mirror_rom_8000_BFFF = header.PrgRomSize == 16000;
   }
}
