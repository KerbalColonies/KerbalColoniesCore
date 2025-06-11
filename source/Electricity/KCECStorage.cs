using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.Electricity
{
    public interface KCECStorage
    {
        double ECStored { get; set; }
        double ECCapacity { get; set; } 

        int ECStoragePriority { get; set; }

        double StoredEC(double lastTime, double deltaTime, double currentTime);

        double ChangeECStored(double deltaEC);

        void SetStoredEC(double storedEC);
    }
}
