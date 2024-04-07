using AutoFixture;
using static Mnes.Core.Machine.CPU.CpuRegisters;

namespace Mnes.Tests.Testing;

public static class AutoFixtureExtensions {
   public static IFixture RegisterList<T>(
      this IFixture me,
      IReadOnlyList<T> list
   ) {
      me.Register(() => list[me.Create<int>() % list.Count]);
      return me;
   }

   public static IFixture MnesFixes(
      this IFixture me
   ) =>
      me
      .RegisterList(RegisterType.Values)
      .RegisterList(StatusFlag.Values);
}
