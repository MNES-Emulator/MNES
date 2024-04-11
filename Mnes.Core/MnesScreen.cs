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
}
