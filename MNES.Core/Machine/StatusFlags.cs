using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mnes.Core.Machine;

public static class StatusFlags
{
    public const byte CARRY = 0b00000001;
    public const byte ZERO = 0b00000010;
    public const byte INTERRUPT_DISABLE = 0b00000100;
    public const byte DECIMAL = 0b00001000;

    public const byte B = 0b00010000;
    public const byte UNUSED = 0b00100000;
    public const byte OVERFLOW = 0b01000000;
    public const byte NEGATIVE = 0b10000000;

}
