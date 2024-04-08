using Godot;
using Mnes.Core.Saves.Configuration;
using Mnes.Ui.Godot.Nodes.Ui;

namespace Mnes.Ui.Godot.Nodes;

public partial class MainScene : Node2D {
   // Idk if I like the Singleton model, I picked it up from Unity devs. Unity produces bad developers.
   public static MainScene Instance { get; private set; }

   public MainUI Ui { get; private set; }

   public override void _Ready() {
      Instance = this;
      Config.InitializeFromDisk();
      Ui = GetNode<MainUI>("CanvasLayer/Main UI");
   }

   public void AddGamesDirectory(string folder) {
   }
}
