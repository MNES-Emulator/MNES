using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Diagnostics;

namespace Mnes.Core.Machine;

public sealed class NesTimer {
   public const int PAL_TIMING_HZ = 1660000;
   public const int NTSC_TIMING_HZ = 1790000;

   [JsonConverter(typeof(StringEnumConverter))]
   public enum RegionType {
      PAL = PAL_TIMING_HZ,
      NTSC = NTSC_TIMING_HZ,
      DUAL_COMPATIBLE,
   }

   public Task RunningThread { get; private set; }
   Action tick_callback;
   int _hertz;
   RegionType _region_backing = RegionType.NTSC;
   bool _is_running;
   bool _is_paused;
   bool _is_stopping;
   public long TotalTickCount { get; private set; }

   RegionType Region {
      get => _region_backing;
      set {
         if (value == RegionType.NTSC) _hertz = NTSC_TIMING_HZ;
         else if (value == RegionType.PAL) _hertz = PAL_TIMING_HZ;
      }
   }

   public NesTimer(RegionType region, Action tick) {
      Region = region;
      tick_callback = tick;
   }

   /// <summary> Starts or unpauses the timer. </summary>
   public void Start() {
      if (_is_running && _is_paused) {
         _is_paused = false;
         return;
      }
      if (_is_running) throw new Exception($"{nameof(NesTimer)} is already running.");
      RunningThread = Task.Run(Run);
   }

   void Run() {
      _is_running = true;

      var stop_watch = new Stopwatch();

      var ticks_per_ms = _hertz / 1000;

      void reset_state() {
         TotalTickCount = 0;
         _is_running = false;
         _is_paused = false;
         _is_stopping = false;
      }

      stop_watch.Start();
      while (_is_running) {
         Thread.Sleep(1);
         var elapsed = stop_watch.Elapsed;
         if (_is_stopping) {
            reset_state();
            return;
         }
         while (_is_paused) {
            Thread.Sleep(1);
            if (_is_stopping) {
               reset_state();
               return;
            }
         }
         stop_watch.Restart();

         var tick_count = (int)(ticks_per_ms * elapsed.TotalMilliseconds);
         for (var i = 0; i < tick_count; i++) tick_callback();
         TotalTickCount += tick_count;
      }
   }

   public void Pause() {
      if (_is_paused) throw new Exception($"{nameof(NesTimer)} is already paused.");
      if (!_is_running) throw new Exception($"Cannot pause {nameof(NesTimer)} when it's not running.");
      _is_paused = true;
   }

   public void Reset() {
      if (!_is_running) throw new Exception($"Cannot reset {nameof(NesTimer)} when it's not running.");
      _is_stopping = true;
      RunningThread.Wait();
      Start();
   }

   public void Stop() {
      if (!_is_running) throw new Exception($"Cannot stop {nameof(NesTimer)} when it's not running.");
      _is_stopping = true;
      RunningThread.Wait();
   }
}
