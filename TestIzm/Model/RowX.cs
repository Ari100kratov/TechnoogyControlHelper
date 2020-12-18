using System;
using System.Collections.Generic;
using System.Linq;
using TestIzm;

namespace WindowsFormsApp1
{
    public class RowX
    {
        public int Id { get; set; }
        public int MachineParamId { get; set; }

        public string Name { get; set; }

        public List<AdditionalSignalRepository.AdditionalSignalRow> SignalList { get; set; }

        public string NameWithCount => $"{this.Name}{(this.SignalList is null || this.SignalList.Count() == 0 ? string.Empty : this.SignalList.Count.ToString())}";

        internal RowX Clone()
        {
            var row = new RowX();
            row.Id = this.Id;
            row.MachineParamId = this.MachineParamId;
            row.Name = this.Name;
            row.SignalList = this.SignalList.ToList();

            return row;
        }
    }
}