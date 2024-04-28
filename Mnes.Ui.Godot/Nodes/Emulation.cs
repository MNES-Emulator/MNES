using Emu.Core;
using Godot;

namespace Mnes.Ui.Godot.Nodes;

public partial class Emulation : Node2D
{
   ImageTexture _texture;
   public Emulator Instance { get; private set; }
   Image image;

   public bool IsRunning => Instance?.IsRunning ?? false;

   public Emulator Emulator { 
      get => Instance; 
      set {
         if (Instance == value) return;
         Instance?.Dispose();
         _texture?.Dispose();
         Instance = value;
         _texture = new();

         image?.Dispose();
         image = Image.CreateFromData(
            width: Instance.Screen.Width,
            height: Instance.Screen.Height,
            useMipmaps: false,
            format: Image.Format.Rgba8,
            data: Instance.Screen.Buffer
            );

         _texture.SetImage(image);
      }
   }

   public override void _Process(double delta)
   {
      QueueRedraw();
      base._Process(delta);
   }

   public override void _Draw()
   {
      if (Instance == null || _texture == null) return;
      // This is probably absolutely horrible, but it works
      image?.Dispose();
      image = Image.CreateFromData(
         width: Instance.Screen.Width,
         height: Instance.Screen.Height,
         useMipmaps: false,
         format: Image.Format.Rgba8,
         data: Instance.Screen.Buffer
         );

      _texture.Update(image);
      DrawTexture(_texture, new Vector2());
      base._Draw();
   }

   protected override void Dispose(bool disposing) {
      Instance?.Dispose();
      _texture?.Dispose();
      base.Dispose(disposing);
   }
}
