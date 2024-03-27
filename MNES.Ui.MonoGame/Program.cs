using System.IO;

using var game = new Mnes.Game1();

if (args.Length > 0)
   if (File.Exists(args[0])) game.StartupRom = args[0];

game.Run();
