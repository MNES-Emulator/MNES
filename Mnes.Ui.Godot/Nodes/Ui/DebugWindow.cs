using Godot;

namespace Mnes.Ui.Godot.Nodes.Ui;

public sealed partial class DebugWindow : Window {
   const string DEBUG_OUTPUT_ID = "%Debug Output";

   TextEdit? _debugOutput;

   public override void _Ready() =>
      _debugOutput = GetNode<TextEdit>(DEBUG_OUTPUT_ID);

   public override void _Process(double delta) {
      var emulator = MainScene.Instance.Ui.Emulation.Emulator;
      if (!emulator.IsRunning) return;

      //var sb = new StringBuilder();
      //foreach (var log in emulation.)
   }
}
