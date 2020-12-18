using System;
using System.Collections.Generic;
using System.Linq;
using TestIzm;
using TestIzm.Model.Db;

namespace WindowsFormsApp1
{
    public class UpModel
    {

        public OperatingProgramExecutionLog Up { get; }

        public int Id { get; }

        public List<Mark> AllMarks { get; set; } /* все оценки */

        public Dictionary<UpModel, double> RangesTime { get; set; }

        public double Time { get; set; }

        /// <summary>
        /// Разница во времени по сравнению с другими циклами.
        /// </summary>
        public double AvgDist { get; internal set; }

        /// <summary>
        /// Качество цикла и наличие аномалий - тут мы смотрим
        /// </summary>
        public UpTypeEnum UpType { get; internal set; }

        public int Index { get; set; }

        public List<RowX> Data { get; set; }

        public bool IsMost { get; internal set; }

        public UpModel(OperatingProgramExecutionLog up, List<RowX> data, int index)
        {
            this.Id = up.Id;
            this.Up = up;
            this.Index = index;
            this.UpType = UpModel.UpTypeEnum.Other;
            this.Time = Math.Round((double)(up.ProcessingTime ?? 0) / 60000, 2);

        }

        public Mark GetMark(int paramId)
        {
            if (paramId == 0)
                return this.AllMarks.FirstOrDefault();

            return this.AllMarks.FirstOrDefault(am => am.ParamId == paramId);
        }

        public enum UpTypeEnum
        {
            Other = 0,
            Good = 1,
            Warning = 2,
            Error = 3
        }

        public void CalcAllMarks()
        {
            this.AllMarks = new List<Mark>();

            foreach (var rowX in this.Data)
            {
                var mark = new Mark(rowX.Id);

                if (rowX.SignalList.Count > 0)
                {
                    var avg = rowX.SignalList.Average(a => a.Value);
                    mark.Avg = avg;
                    mark.Count = rowX.SignalList.Count();
                    mark.Min = rowX.SignalList.Min(a => a.Value);
                    mark.Max = rowX.SignalList.Max(a => a.Value);
                    mark.AvgDiff = rowX.SignalList.Average(a => Math.Abs(avg - a.Value));
                    mark.Disp = Math.Sqrt(rowX.SignalList.Average(a => Math.Pow(avg - a.Value, 2)));
                }

                this.AllMarks.Add(mark);
            }
        }
    }
}
