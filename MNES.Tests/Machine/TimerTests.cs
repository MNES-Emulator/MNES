using Mnes.Core.Machine;
using System.Diagnostics;

namespace Mnes.Tests.Machine;

[TestClass]
public sealed class TimerTests {
   [TestMethod]
   public void TestTimer() {
      ulong tick_count = 0;
      ulong seconds_count = 0;
      var stopwatch = new Stopwatch();
      stopwatch.Start();

      void tick() {
         if (tick_count++ % NesTimer.NTSC_TIMING_HZ == 0) {
            Debug.WriteLine($"{nameof(NesTimer)} seconds elapsed: {seconds_count++}");
            Debug.WriteLine($"{nameof(Stopwatch)} seconds elapsed: {stopwatch.Elapsed.TotalSeconds}");
         }
      }

      var nes_timer = new NesTimer(NesTimer.RegionType.NTSC, tick);
      nes_timer.Start();
      Thread.Sleep(TimeSpan.FromSeconds(10));
      Debug.WriteLine("Pause for 2 seconds...");
      nes_timer.Pause();
      Thread.Sleep(TimeSpan.FromSeconds(2));
      Debug.WriteLine("Start.");
      nes_timer.Start();
      Thread.Sleep(TimeSpan.FromSeconds(10));
      Debug.WriteLine("Reset.");
      nes_timer.Reset();
      Thread.Sleep(TimeSpan.FromSeconds(5));
      Debug.WriteLine("Done.");
   }
}
