using System;
using System.Runtime.Serialization;
using System.Text;

namespace TestIzm
{
    public partial class AdditionalSignal
    {
        public DateTime ReceiveDate { get; set; }
        public int MachineID { get; set; } /* - */
        public int SignalType { get; set; } /* !? */
        public double? SignalValue { get; set; }
        public int ParamInMachineID { get; set; } /* - */
    }
}