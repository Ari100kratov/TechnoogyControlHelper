using System.Collections.Generic;
using System.Linq;
using System.Data;
using TestIzm.Model.Db;

namespace TestIzm
{
    public class MachineModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<ParamInMachineModel> ParameterList { get; set; }

        public MachineModel()
        {
        }

        public MachineModel(Machine m)
        {
            this.Id = m.ID;
            this.Name = m.Name;
            this.ParameterList = m.ParamInMachine2.ToList().Where(pim => pim.MachineParam.MachineParamTypeID == 3).Select(pim => new ParamInMachineModel(pim)).ToList();
        }
    }
}



