using DevExpress.Utils.DirectXPaint;
using DevExpress.XtraBars;
using DevExpress.XtraCharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestIzm.Model;
using TestIzm.Model.Db;
using WindowsFormsApp1;
using static TestIzm.AdditionalSignalRepository;

namespace TestIzm.Bl
{
    public class AnalizeTool
    {
        public MachineModel Machine { get; private set; }
        public Dictionary<int, List<AdditionalSignalRow>> DataSource { get; private set; }

        private string urlFiles;

        public AnalizeTool(string urlFiles)
        {
            DataManager.Init();

            this.urlFiles = urlFiles;
        }

        public void Load(DateTime dtFrom, DateTime dtTo, MachineModel machine)
        {
            this.Machine = machine;

            var rep = new AdditionalSignalRepository(urlFiles);
            var listx = rep.GetFastList(machine.Id, dtFrom, dtTo);
            // если данных нет - можно запросить у нашего сервера и закешировать.

            this.DataSource = listx.GroupBy(a => (int)a.ParamId).ToDictionary(g => g.Key, g => g.ToList());

            var cncLog = DataManager.CncLog.Where(cnc => cnc.Machine.ID == this.Machine.Id && cnc.DtEnd >= dtFrom && cnc.DtStart <= dtTo).ToList();

            foreach (var val in listx)
                val.Datetime = new DateTime(2000, 1, 1).AddSeconds(val.DatetimeSeconds);

            var upList = new List<UpModel>();
            var analisisList = new List<UpSeriesAnalisis>();
            var upCurrentName = cncLog.First().NameUP;
            var dtCurrent = dtFrom;
            var mainParamId = listx.FirstOrDefault()?.ParamId ?? 0;

            var rowXList = new List<RowX>();

            foreach (var group in this.DataSource)
            {
                var rowX = new RowX()
                {
                    Id = group.Key,
                    Name = this.Machine.ParameterList.FirstOrDefault(p => p.Id == group.Key)?.Name ?? group.Key.ToString(),
                    List = group.Value
                };

                rowXList.Add(rowX);
            }

            var rowXIndexes = rowXList.Select(r => (int)0).ToArray();

            foreach (var up in cncLog)
            {
                var model = new UpModel(up, new List<RowX>(), upList.Count + 1);

                if (upCurrentName != up.NameUP)
                {
                    var analisisRowXList = new List<RowX>();

                    // загружаем туда данные телеметрии все с dict[param] as index до dtTo.
                    // причем можно сразу писать часть данных сразу в УП.

                    for (int rowIndex = 0; rowIndex < rowXList.Count; rowIndex++)
                    {
                        var rowX = rowXList[rowIndex];
                        var newRowX = new RowX()
                        {
                            Id = rowX.Id,
                            Name = rowX.Name
                        };

                        var fromIndex = rowXIndexes[rowIndex];
                        int to = rowX.List.Count;

                        for (int i = fromIndex; i < rowX.List.Count; i++)
                        {
                            if (rowX.List[i].Datetime >= up.DtStart)
                            {
                                to = i > 0 ? (i - 1) : 0;
                                rowXIndexes[rowIndex] = i;
                                break;
                            }
                        }

                        newRowX.List = rowX.List.Skip(fromIndex).Take(to - fromIndex).ToList();

                        analisisRowXList.Add(newRowX);
                    }

                    analisisList.Add(new UpSeriesAnalisis(dtCurrent, up.DtStart, upCurrentName, upList, analisisRowXList) { MainParamId = mainParamId });

                    upList = new List<UpModel>();
                    dtCurrent = up.DtStart;
                    upCurrentName = up.NameUP;
                }

                upList.Add(model);
            }

            this.UpSeriesAnalisisList = analisisList.ToList();
            this.UpSeriesCombinedAnalisisList = new List<UpSeriesAnalisis>();

            if (analisisList.Count == 0)
                return;

            var host = this.UpSeriesAnalisisList[0].Clone();
            this.UpSeriesAnalisisList[0].HostAnalisist = host;
            this.UpSeriesCombinedAnalisisList.Add(host);

            host = this.UpSeriesAnalisisList[1].Clone();
            this.UpSeriesAnalisisList[1].HostAnalisist = host;
            this.UpSeriesCombinedAnalisisList.Add(host);

            // теперь попробуем объединить 
            for (int i = 1; i < this.UpSeriesAnalisisList.Count - 1; i++)
            {
                var beforeAnalisist = this.UpSeriesAnalisisList[i - 1];
                var nextAnalisist = this.UpSeriesAnalisisList[i + 1];

                if (nextAnalisist.UpName == beforeAnalisist.UpName)
                {
                    beforeAnalisist.HostAnalisist.Add(nextAnalisist);
                    nextAnalisist.HostAnalisist = beforeAnalisist.HostAnalisist;
                }

                if (nextAnalisist.HostAnalisist == null)
                {
                    host = nextAnalisist.Clone();
                    nextAnalisist.HostAnalisist = host;
                    this.UpSeriesCombinedAnalisisList.Add(host);
                }
            }
        }

        internal void SetMainParam(int id)
        {
            foreach (var series in this.UpSeriesAnalisisList)
            {
                series.ColorizeMost(id);
            }
        }

        /// <summary>
        /// Плохой метод
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal List<AdditionalSignalRow> GetTelemetry(int id)
        {
            this.DataSource.TryGetValue(id, out var list);

            return list ?? new List<AdditionalSignalRow>();
        }

        public List<UpSeriesAnalisis> UpSeriesAnalisisList { get; private set; }
        public List<UpSeriesAnalisis> UpSeriesCombinedAnalisisList { get; private set; }

        public UpSeriesAnalisis GetAnalisis(DateTime dt)
        {
            /* подумать куда мы попали - в переходную УП или в полную */

            // var analisisList = this.UpSeriesCombinedAnalisisList.Where(up => up.DtFrom < dt && up.DtTo > dt).ToList();
            // var analisis = analisisList.FirstOrDefault();
            // 
            // foreach (var series in analisisList)
            // {
            //     var upModel = series.UpModelList.FirstOrDefault(up => up.Up.DtStart <= dt && up.Up.DtEnd >= dt);
            // 
            //     if (upModel != null)
            //     {
            //         analisis = series;
            //         break;
            //     }
            // }

            var analisis = this.UpSeriesAnalisisList?.LastOrDefault(up => up.DtFrom < dt);

            if (analisis == null)
                return null;

            analisis = analisis.HostAnalisist ?? analisis;

            if (!analisis.IsCalculated)
                analisis.Calculate();

            return analisis;
        }

        public UpModel GetUpModel(DateTime lastMouseDatetime)
        {
            var analisis = GetAnalisis(lastMouseDatetime);

            var model = analisis?.UpModelList?.LastOrDefault(up => up.Up.DtStart < lastMouseDatetime);

            return model;
        }
    }
}
