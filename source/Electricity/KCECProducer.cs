using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.Electricity
{
    public interface KCECProducer
    {
        double ECProduction(double deltaTime);

        double DailyECProduction();
    }
}
