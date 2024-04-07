using System.Collections.Immutable;

namespace Mnes.Core.Machine.CPU;

partial class CpuRegisters {
   public sealed class RegisterType {
      static readonly List<RegisterType> sValues = new();

      public static readonly RegisterType A = new('A', setsFlags: true);
      public static readonly RegisterType X = new('X', setsFlags: true);
      public static readonly RegisterType Y = new('Y', setsFlags: true);
      public static readonly RegisterType S = new('S', setsFlags: false);
      public static readonly RegisterType P = new('P', setsFlags: false);

      public static IReadOnlyList<RegisterType> Values { get; } = sValues.ToImmutableList();

      readonly int _index;

      public char Name { get; }
      public bool SetsFlags { get; }

      RegisterType(
         char name,
         bool setsFlags
      ) {
         _index = sValues.Count;
         Name = name;
         SetsFlags = setsFlags;

         sValues.Add(this);
      }

      public static implicit operator int(RegisterType me) => me._index;
   }
}
