using Emu.Core;
using Godot;

namespace Mnes.Ui.Godot.Nodes;

public sealed partial class Emulation : Node2D {
   ImageTexture _texture;
   Emulator _emulator;
   Image image;

   public Emulator Emulator {
      get => _emulator;
      set {
         if (_emulator == value) return;
         _emulator?.Dispose();
         _texture?.Dispose();
         _emulator = value;
         _texture = new();

         image?.Dispose();
         image = Image.CreateFromData(
            width: _emulator.Screen.Width,
            height: _emulator.Screen.Height,
            useMipmaps: false,
            format: Image.Format.Rgba8,
            data: _emulator.Screen.Buffer
         );

         _texture.SetImage(image);
      }
   }

   public override void _Process(double delta) {
      QueueRedraw();
      base._Process(delta);
   }

   public override void _Draw() {
      if (_emulator == null || _texture == null) return;
      // This is probably absolutely horrible, but it works
      image?.Dispose();
      image = Image.CreateFromData(
         width: _emulator.Screen.Width,
         height: _emulator.Screen.Height,
         useMipmaps: false,
         format: Image.Format.Rgba8,
         data: _emulator.Screen.Buffer
      );

      _texture.Update(image);
      DrawTexture(_texture, new Vector2());
      base._Draw();
   }

   protected override void Dispose(bool disposing) {
      _emulator?.Dispose();
      _texture?.Dispose();
      base.Dispose(disposing);
   }
}
