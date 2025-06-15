using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.Electricity
{
    public interface KCECStorage
    {
        double ECStored { get; }
        double ECCapacity { get; } 

        int ECStoragePriority { get; }

        /// <summary>
        /// The provided times might be used to calculate background loss
        /// </summary>
        double StoredEC(double lastTime, double deltaTime, double currentTime);

        double ChangeECStored(double deltaEC);

        void SetStoredEC(double storedEC);
    }
}
