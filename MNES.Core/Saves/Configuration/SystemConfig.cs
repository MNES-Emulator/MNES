using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mnes.Core.Machine.NesTimer;

namespace Mnes.Core.Saves.Configuration
{
    public class SystemConfig
    {
        public RegionType Region { get; set; } = RegionType.NTSC;

        public bool DebugMode { get; set; }
    }
}
