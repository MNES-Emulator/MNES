using Emu.Core;

namespace Mnes.Core;

public sealed class MnesScreen : EmulatorScreen
{
   public override int Width { get; } = 256;
   public override int Height { get; } = 240;
   public override byte[] Buffer { get; } = GetBuffer();

   static byte[] GetBuffer() {
      var buffer = new byte[256 * 240 * 4];
      for (int i = 0; i < buffer.Length; i += 4) {
         buffer[i + 0] = 255;
         buffer[i + 1] = 0;
         buffer[i + 2] = 0;
         buffer[i + 3] = 255;
      }
      return buffer;
   }

   public void WriteRgb(int x, int y, int rgb) =>
      WriteRgb(x + y * Height, rgb);

   public void WriteRgb(int i, int rgb) {
      i *= 4;

      // The order of this is to be seen
      Buffer[i + 0] = (byte)(rgb >> 0); // r
      Buffer[i + 1] = (byte)(rgb >> 8); // g
      Buffer[i + 2] = (byte)(rgb >> 16); // b
      Buffer[i + 3] = 255; // a
   }
}
