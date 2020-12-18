using System;
using System.Collections.Generic;
using System.Linq;
using TestIzm;

namespace WindowsFormsApp1
{
    public class RowX
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public List<AdditionalSignalRepository.AdditionalSignalRow> List { get; set; }

        public override string ToString()
        {
            return Name + " " + List.Count;
        }

        internal RowX Clone()
        {
            var row = new RowX();
            row.Id = this.Id;
            row.Name = this.Name;
            row.List = this.List.ToList();

            return row;
        }
    }
}