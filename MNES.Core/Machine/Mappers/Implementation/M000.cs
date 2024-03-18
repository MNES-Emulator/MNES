using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNES.Core.Machine.Mappers.Implementation
{
    internal class M000 : Mapper
    {
        public override int MapperNumber => 0;

        protected override MapperBank[] Banks { get; } = new MapperBank[] { 
            new(MapperBank.AccessorType.CPU, MapperBank.BankType.PRG_RAM, new Range(0x6000, 0x8000), new(0x0000, 0x2000))
        };

        public override ushort this[ushort index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public M000(MachineState machine) : base(machine) { }


    }
}
