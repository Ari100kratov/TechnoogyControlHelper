using System;
using System.Collections.Generic;
using System.Linq;
using TestIzm.Model.Db;

namespace TestIzm
{
    public static class DataManager
    {
        public static List<MachineModel> Machines { get; set; }
        public static List<OperatingProgramExecutionLog> CncLog { get; internal set; }

        public static void Init()
        {
            var db = new IndustryDBEntities1();
            DataManager.Machines = db.Machine.ToList().Select(mm => new MachineModel(mm)).ToList();
            DataManager.CncLog = db.OperatingProgramExecutionLog.OrderBy(l => l.DtStart).ToList();
        }
    }
}



