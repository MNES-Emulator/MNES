using Godot;
using Mnes.Ui.Godot.Nodes;
using System.Text;

public partial class DebugWindow : Window
{
   const string DEBUG_OUTPUT_ID = "%Debug Output";
   TextEdit DebugOutput;

	public override void _Ready()
	{
      DebugOutput = GetNode<TextEdit>(DEBUG_OUTPUT_ID);
	}

	public override void _Process(double delta)
	{
      var emulation = MainScene.Instance.Ui.emulation.Instance;
      if (!emulation.IsRunning) return;

      var sb = new StringBuilder();
      foreach (var log in emulation.)
	}
}
