using Mnes.Ui.MonoGame;

var maybeFilename = File.Exists(args.FirstOrDefault()) ? args[0] : null;

using var game = new Game1(maybeFilename);

game.Run();
