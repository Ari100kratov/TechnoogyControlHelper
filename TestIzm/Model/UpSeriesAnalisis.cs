using DevExpress.XtraCharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestIzm.Model.Db;
using WindowsFormsApp1;

namespace TestIzm.Model
{
    public class UpSeriesAnalisis
    {
        public string UpName { get; }
        public DateTime DtFrom { get; }
        public DateTime DtTo { get; private set; }
        public int EtalonNumber { get; private set; }
        // public List<OperatingProgramExecutionLog> ProgramLogList { get; }
        public List<RowX> Data { get; }

        public List<UpModel> UpModelList { get; }
        public int MainParamId { get; set; }
        public bool IsCalculated { get; internal set; }
        public double MostAvgTime { get;  set; }
        public double AvgTime { get; private set; }
        public double UCLTime { get;  set; }
        public double LCLTime { get;  set; }
        public double UCL { get;  set; }
        public double LCL { get;  set; }
        public double UCLWarning { get;  set; }
        public double LCLWarning { get;  set; }
        public double Avg { get;  set; }
        public double DeltaAvg { get; private set; }
        public UpSeriesAnalisis HostAnalisist { get; internal set; }

        public UpSeriesAnalisis(
            DateTime dtFrom,
            DateTime dtTo,
            string upName,
            List<UpModel> upModelList,
            List<RowX> data)
        {
            this.IsCalculated = false;
            this.UpModelList = upModelList ?? new List<UpModel>();
            this.Data = data;
            this.UpName = upName;

            this.DtFrom = dtFrom;
            this.DtTo = dtTo;

            // будем искать корреляцию между параметрами?
        }

        public void Calculate()
        {
            // выносим результаты на отдельный лист.            
            // для начала пройдемся по всем УП и посчитаем какие из них "левые", а какие норм
            if (this.UpModelList.Count == 0)
                return;

            this.FillData();
            this.CalcMosted();

            this.RecalcLevels();

            this.ColorizeMost(this.MainParamId);

            // Надо посчиать сигму. Пока как среднеквадративное отклонение
            // Проверить что на мм и км одинаковые результаты.

            // Теперь посчитаем UCL И LCL - верхний и нижний пределы. Хорошо бы 
            // Если UCL - LCL / 6 сигма  больше 1 - то все супер. Стабилизировали процесс, он управляем и в нем нечего менять.
            // Если меньше 1 - то плохо. Чем ближе к 0 тем хуже. В этих случаях нужно разбирать причины отклонений, фиксировать их и работать с ними. 

            // Покажите плотность распределения

            // Причины по демингу сводятся не только к деньгам, ошибкам оператора и проблемам в технологии - там и качество материала, поставщиков, инструмента, 
            // квалификация персонала, мотивация персонала, вовлеченность в процесс, удовлетворение от работы (Кайдзен). Причины определить сложно

            // Статистическое управление процессами по Демингу.

            // Для работы с причиныами отклонений хорошо бы применять правило Паретто.    
            // Правило Паретто позволяет работать с наибольшими причинами. Выявлять их и фокусировать управленческие решения на них.
            // Причины могут быть различные.

            this.IsCalculated = true;
        }

        public void RecalcLevels()
        {
            // надо найти диапозон который покрывает 70% всех уп
            // остальное 70% - желтое
            // остальное - красное
            this.Avg = this.UpModelList.Where(m => m.IsMost).Average(m => m.GetMark(this.MainParamId)?.Avg??0);
            this.DeltaAvg = this.UpModelList.Where(m => m.IsMost).Average(m => Math.Abs(this.Avg - m.GetMark(this.MainParamId).Avg)); //  может использовать квадратичное отклонение?

            this.LCL = this.Avg - 2 * this.DeltaAvg;
            this.UCL = this.Avg + 2 * this.DeltaAvg;
            this.LCLWarning = this.Avg - 1.1 * this.DeltaAvg;
            this.UCLWarning = this.Avg + 1.1 * this.DeltaAvg;
        }

        public void CalcMosted()
        {
            var listUp = this.UpModelList;
            this.AvgTime = listUp.Average(m => m.Time);

            if (listUp.Count < 3)
            {
                foreach (var up in listUp)
                    up.IsMost = true;

                this.MostAvgTime = this.AvgTime;

                return;
            }

            // найти наиболее частое максимальное время выполнения
            // отсеять сильно отличающиеся по времени 
            foreach (var upModel in listUp)
            {
                upModel.RangesTime = new Dictionary<UpModel, double>();

                foreach (var up2 in listUp)
                    if (up2 != upModel)
                        upModel.RangesTime.Add(up2, Math.Abs(up2.Time - upModel.Time));

                upModel.AvgDist = upModel.RangesTime.Values.Average();
            }

            // найти идеальные параметры
            var most = listUp.OrderBy(p => p.AvgDist).Take((int)listUp.Count / 2).ToList();

            this.MostAvgTime = most.Average(m => m.Time);
            this.UCLTime = 1.5 * this.MostAvgTime;
            this.LCLTime = 0.5 * this.MostAvgTime;
            // todo: продумать когда mostAvgTime сильно меньше AvgTime !?

            RecalcMosted();
        }

        internal void Add(UpSeriesAnalisis nextAnalisist)
        {
            this.DtTo = nextAnalisist.DtTo;

            foreach (var row in nextAnalisist.Data)
            {
                var thisRow = this.Data.FirstOrDefault(r => r.Id == row.Id);
                
                if (thisRow == null)
                    continue;

                thisRow.List.AddRange(row.List);
            }

            this.UpModelList.AddRange(nextAnalisist.UpModelList);

            int index = 1;

            foreach(var up in this.UpModelList)
            {
                up.Index = index++;
            }
        }

        internal UpSeriesAnalisis Clone()
        {
            var listRowX = new List<RowX>();

            foreach (var row in this.Data)
                listRowX.Add(row.Clone());

            var analisis = new UpSeriesAnalisis(this.DtFrom, this.DtTo, this.UpName, this.UpModelList.ToList(), listRowX);

            return analisis;
        }

        public void RecalcMosted()
        {
            foreach (var upModel in this.UpModelList) // тут нужна картинка распределения вероятности распределения
            {
                if (upModel.Time <= this.UCLTime && upModel.Time >= this.LCLTime)
                    upModel.IsMost = true;
                else
                    upModel.IsMost = false;
            }

            if (this.UpModelList.Count(m => m.IsMost) <= 1)
            {
                foreach (var up in this.UpModelList)
                    up.IsMost = true;

                return;
            }
        }

        public void ColorizeMost(int? paramId = null)
        {

            if (paramId == null)
                paramId = this.MainParamId;
            else
                this.MainParamId = paramId.Value;

            if (!this.IsCalculated)
                return;


            foreach (var upModel in this.UpModelList)
            {
                if (upModel.IsMost)
                {
                    var avgUp = upModel.GetMark(this.MainParamId).Avg;
                    upModel.UpType = UpModel.UpTypeEnum.Good;

                    if (avgUp >= this.UCLWarning || avgUp <= this.LCLWarning)
                        upModel.UpType = UpModel.UpTypeEnum.Warning;

                    if (avgUp >= this.UCL || avgUp <= this.LCL)
                        upModel.UpType = UpModel.UpTypeEnum.Error;
                }
                else
                    upModel.UpType = UpModel.UpTypeEnum.Other;
            }
        }

        private void FillData()
        {
            var rowXIndexes = this.Data.Select(r => (int)0).ToArray();

            foreach (var upModel in this.UpModelList)
            {
                upModel.Data = new List<RowX>();
                for (int rowIndex = 0; rowIndex < this.Data.Count; rowIndex++)
                {
                    var rowX = this.Data[rowIndex];
                    var newRowX = new RowX()
                    {
                        Id = rowX.Id,
                        Name = rowX.Name
                    };

                    var fromIndex = rowXIndexes[rowIndex];
                    var to = rowX.List.Count;

                    for (int i = fromIndex; i < rowX.List.Count; i++)
                    {
                        if (rowX.List[i].Datetime <= upModel.Up.DtStart)
                        {
                            fromIndex = i;
                        }

                        if (rowX.List[i].Datetime > upModel.Up.DtEnd)
                        {
                            rowXIndexes[rowIndex] = i;
                            to = i;
                            break;
                        }
                    }
                    newRowX.List = rowX.List.Skip(fromIndex).Take(to - fromIndex).ToList();

                    upModel.Data.Add(newRowX);
                }

                upModel.CalcAllMarks();
            }
        }
    }
}
