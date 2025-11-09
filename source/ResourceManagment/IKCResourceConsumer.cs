using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.ResourceManagment
{
    public interface IKCResourceConsumer
    {
        Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime);
        void ConsumeResources(double lastTime, double deltaTime, double currentTime);

        /// <summary>
        /// Gets called when there are insufficient resources to meet the expected consumption.
        /// </summary>
        /// <returns>MUST return unused resources, otherwise they are lost</returns>
        Dictionary<PartResourceDefinition, double> InsufficientResources(double lastTime, double deltaTime, double currentTime, Dictionary<PartResourceDefinition, double> sufficientResources, Dictionary<PartResourceDefinition, double> limitingResources);
        Dictionary<PartResourceDefinition, double> DailyResourceConsumption();
        int ResourceConsumptionPriority { get; }
    }
}
