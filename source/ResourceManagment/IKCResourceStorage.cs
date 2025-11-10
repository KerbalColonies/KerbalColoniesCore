using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.ResourceManagment
{
    public interface IKCResourceStorage
    {
        SortedDictionary<PartResourceDefinition, double> Resources { get; }

        double Volume { get; }
        double UsedVolume { get; }
        int Priority { get; }

        double MaxStorable(PartResourceDefinition resource);
        SortedDictionary<PartResourceDefinition, double> StoredResources(double lastTime, double deltaTime, double currentTime);
        double ChangeResourceStored(PartResourceDefinition resource, double Amount);
    }
}
