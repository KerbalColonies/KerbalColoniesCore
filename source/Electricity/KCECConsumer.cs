using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.Electricity
{
    public interface KCECConsumer
    {
        double ExpectedECConsumption(double lastTime, double deltaTime, double currentTime);
        void ConsumeEC(double lastTime, double deltaTime, double currentTime);
        void ÍnsufficientEC(double lastTime, double deltaTime, double currentTime, double remainingEC);
        double DailyECConsumption();

        int ECConsumptionPriority { get; }
    }
}
