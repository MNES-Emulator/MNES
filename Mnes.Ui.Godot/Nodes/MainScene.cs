using Godot;
using Mnes.Core.Saves.Configuration;
using Mnes.Ui.Godot.Nodes.Ui;
using System.IO;
using System.Linq;

namespace Mnes.Ui.Godot.Nodes;

public partial class MainScene : Node2D {
   // Idk if I like the Singleton model, I picked it up from Unity devs. Unity produces bad developers.
   public static MainScene Instance { get; private set; }

   public MainUI Ui { get; private set; }

   string arg_game;

   public override void _Ready() {
      Instance = this;
      Config.InitializeFromDisk();
      Ui = GetNode<MainUI>("CanvasLayer/Main UI");
      var arg = OS.GetCmdlineArgs().FirstOrDefault();
      if (!string.IsNullOrWhiteSpace(arg) && File.Exists(arg) && Path.GetExtension(arg).ToLower() == ".nes")
         arg_game = arg;
   }

   public override void _Process(double delta)
   {
      if (arg_game != null) Ui.FldlRomSelected(arg_game);
      arg_game = null;
      base._Process(delta);
   }

   public void AddGamesDirectory(string folder) {
   }
}
