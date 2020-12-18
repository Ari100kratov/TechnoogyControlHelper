using System.Collections.Generic;
using TestIzm.Model.Db;

namespace TestIzm
{
    public class ParamInMachineModel {

        public int Id { get; set; }
        public int MachineParamID { get; set; }
        public string Name { get; set; }

        public ParamInMachineModel(ParamInMachine pim)
        {
            this.Id = pim.ID;
            this.MachineParamID = pim.MachineParam.ID;
            this.Name = pim.MachineParam.Name;
        }
    }
}



