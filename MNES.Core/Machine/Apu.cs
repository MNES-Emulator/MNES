using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mnes.Core.Machine;

public class Apu
{
    /// <summary> Also contains some IO registers. </summary>
    public byte[] Registers = new byte[32];

    public void SetPowerUpState()
    {
        Array.Clear(Registers);
    }
}
