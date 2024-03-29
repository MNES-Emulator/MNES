using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mnes.Core.Utility
{
   /// <summary> Number with only 3 bits. </summary>
   public struct Byte3
   {
      private ushort _value = 0;

      public Byte3() { }
      public Byte3(ushort value)
      {
         _value = (ushort)(value & 0b_0111_1111_1111_1111);
      }

      public static implicit operator ushort(Byte3 d) => d._value;
      public static explicit operator Byte3(ushort b) => new(b);
   }
}
