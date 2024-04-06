using System.Collections.Immutable;

namespace Mnes.Core.Machine.CPU;

partial class CpuRegisters {
   public readonly record struct RegisterType {
      static readonly List<RegisterType> sValues = new();

      public static readonly RegisterType A = new(0, setsFlags: true);
      public static readonly RegisterType X = new(1, setsFlags: true);
      public static readonly RegisterType Y = new(2, setsFlags: true);
      public static readonly RegisterType S = new(3, setsFlags: false);
      public static readonly RegisterType P = new(4, setsFlags: false);

      public static IReadOnlyList<RegisterType> Values { get; } = sValues.ToImmutableList();

      readonly int _index;
      public bool SetsFlags { get; }
      public char Name => _index switch {
         0 => 'A',
         1 => 'X',
         2 => 'Y',
         3 => 'S',
         4 => 'P',
         _ => throw new Exception()
      };

      public RegisterType()
      : this(A._index, A.SetsFlags) {
      }

      RegisterType(
         int index,
         bool setsFlags
      ) {
         _index = index;
         SetsFlags = setsFlags;

         sValues.Add(this);
      }

      public static implicit operator int(RegisterType me) => me._index;
   }
}
