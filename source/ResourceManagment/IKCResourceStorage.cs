using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.ResourceManagment
{
    public interface IKCResourceStorage
    {
        Dictionary<PartResourceDefinition, double> ResourcesStored { get; }

        double MaxVolume { get; }
        int ResourceStoragePriority { get; }

        double MaxStorable(PartResourceDefinition resource);
        Dictionary<PartResourceDefinition, double> StoredResources(double lastTime, double deltaTime, double currentTime);
        double ChangeResourceStored(PartResourceDefinition resource, double Amount);
        void SetStoredResources(Dictionary<PartResourceDefinition, double> storedResources);
    }
}
