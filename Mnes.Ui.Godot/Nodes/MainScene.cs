using Godot;
using Mnes.Ui.Godot.Nodes.Ui;
using Mnes.Ui.Shared;

namespace Mnes.Ui.Godot.Nodes;

public sealed partial class MainScene : Node2D {
   // Idk if I like the Singleton model, I picked it up from Unity devs. Unity produces bad developers.
   static MainScene? _instance;
   public static MainScene Instance => _instance ??= new();

   MainUI? _ui;
   public MainUI Ui => _ui ?? throw new InvalidOperationException($"{nameof(_ui)} was null");

   string? arg_game;

   MainScene() {
   }

   public override void _Ready() {
      Config.InitializeFromDisk();
      _ui = GetNode<MainUI>("CanvasLayer/Main UI");
      var arg = OS.GetCmdlineArgs().FirstOrDefault();

      if (File.Exists(arg) && Path.GetExtension(arg).ToLower() == ".nes")
         arg_game = arg;
   }

   public override void _Process(double delta) {
      if (arg_game != null)
         Ui.FldlRomSelected(arg_game);

      arg_game = null;
      base._Process(delta);
   }

   public void AddGamesDirectory(string folder) {
   }
}
