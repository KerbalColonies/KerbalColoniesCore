using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.Electricity
{
    public interface KCECConsumer
    {
        double ECConsumption(double deltaTime);
        double DailyECConsumption();

        int ECConsumptionPriority { get; }
    }
}
