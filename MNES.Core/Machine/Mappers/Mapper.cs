using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNES.Core.Machine.Mappers
{
    // https://www.nesdev.org/wiki/Mapper
    public abstract class Mapper
    {
        protected readonly MachineState Machine;
        public abstract int MapperNumber { get; }

        protected abstract MapperBank[] Banks { get; }

        public Mapper(MachineState machine)
        {
            Machine = machine;
        }

        public abstract ushort this[ushort index] { get; set; }
    }
}
