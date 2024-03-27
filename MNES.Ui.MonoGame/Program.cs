using System.IO;
using Mnes.Ui.MonoGame;

using var game = new Game1();

if (args.Length > 0)
   if (File.Exists(args[0])) game.StartupRom = args[0];

game.Run();
