using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.ResourceManagment
{
    public interface IKCResourceProducer
    {
        Dictionary<PartResourceDefinition, double> ResourceProduction(double lastTime, double deltaTime, double currentTime);
        Dictionary<PartResourceDefinition, double> ResourcesPerSecond();
    }
}
