using System.Collections.Generic;
using System.Numerics;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class ContactSensorArray : ISensorContainer
    {
        public readonly ContactTotalLoadSensor TotalLoad = new();
        public readonly ContactSlipSensor Slip = new();
        public readonly ContactDirectionalLoadSensor XAxisLoad = new(Vector3.UnitX);
        public readonly ContactDirectionalLoadSensor YAxisLoad = new(Vector3.UnitY);
        public readonly ContactDirectionalLoadSensor ZAxisLoad = new(Vector3.UnitZ);

        private readonly List<Sensor> sensors;
        public IEnumerable<Sensor> Sensors => sensors;

        public ContactSensorArray()
        {
            sensors = new List<Sensor>
            {
                TotalLoad,
                Slip,
                XAxisLoad,
                YAxisLoad,
                ZAxisLoad
            };
        }
    }
}
