using BackgroundResourceProcessing.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.VesselAutoTransfer
{
    public class BackgroundKCTransfer : BackgroundConverter<ModuleKCTransfer>
    {
        public override ModuleBehaviour GetBehaviour(ModuleKCTransfer module) => new ModuleBehaviour(new KCTransferBehaviour(module));
    }
}
