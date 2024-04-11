using Emu.Core;
using Godot;

public partial class Emulation : Node2D
{
   ImageTexture _texture;
   Emulator _emulator;

   public Emulator Emulator { 
      get => _emulator; 
      set {
         if (_emulator == value) return;
         _emulator?.Dispose();
         _texture?.Dispose();
         _emulator = value;
         _texture = new();

         _texture.SetImage(Image.CreateFromData(
            width: _emulator.Screen.Width,
            height: _emulator.Screen.Height,
            useMipmaps: false,
            format: Image.Format.Rgba8,
            data: _emulator.Screen.Buffer
            ));
      }
   }

   public override void _Process(double delta)
   {
      QueueRedraw();
      base._Process(delta);
   }

   public override void _Draw()
   {
      if (_emulator == null || _texture == null) return;
      // This is absolutely horrible
      _texture.Update(Image.CreateFromData(
         width: _emulator.Screen.Width,
         height: _emulator.Screen.Height,
         useMipmaps: false,
         format: Image.Format.Rgba8,
         data: _emulator.Screen.Buffer
         ));
      DrawTexture(_texture, new Vector2());
      //DrawCircle(new Vector2(), 10f, Colors.Pink);
      base._Draw();
   }

   protected override void Dispose(bool disposing) {
      _emulator?.Dispose();
      _texture?.Dispose();
      base.Dispose(disposing);
   }
}
