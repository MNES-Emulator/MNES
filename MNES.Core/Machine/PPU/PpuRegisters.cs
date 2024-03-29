using Mnes.Core.Machine.PPU.Registers;

namespace Mnes.Core.Machine.PPU;

// https://www.nesdev.org/wiki/PPU_registers
public sealed class PpuRegisters {
   readonly MachineState machine;
   readonly Register[] registers;

   public byte OpenBus { get; set; }

   public PpuCtrl PPUCTRL => (PpuCtrl)registers[0];
   public PpuMask PpuMask => (PpuMask)registers[1];
   public PpuStatus PpuStatus => (PpuStatus)registers[2];
   public OamAddr OamAddr => (OamAddr)registers[3];
   public OamData OamData => (OamData)registers[4];
   public PpuScroll PpuScroll => (PpuScroll)registers[5];
   public PpuAddr PpuAddr => (PpuAddr)registers[6];
   public PpuData PpuData => (PpuData)registers[7];

   public byte this[int index] {
      get => registers[index].CpuRead();
      set => registers[index].CpuWrite(value);
   }

   public PpuRegisters(MachineState m) {
      machine = m;

      registers = new Register[] {
         new PpuCtrl(m),
         new PpuMask(m),
         new PpuStatus(m),
         new OamAddr(m),
         new OamData(m),
         new PpuScroll(m),
         new PpuAddr(m),
         new PpuData(m),
      };
   }

   public void SetPowerUpState() {
      foreach (var r in registers) r.SetPowerUpState();
   }
}
