namespace Emu.Core;

public abstract class EmulatorScreen {
   public abstract int Width { get; }
   public abstract int Height { get; }
   /// <summary> Rgba8: red green blue alpha with a bit depth of 8. </summary>
   public abstract byte[] Buffer { get; }
}
