using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TestIzm
{
    /// <summary>
    ///     Репозиторий сигналов измеряемых параметров. Работает с файлами, а не с объектами
    /// </summary>
    public class AdditionalSignalRepository
    {
        public class AdditionalSignalRow
        {
            public int DatetimeSeconds;
            public DateTime Datetime;
            public short ParamId;
            public double Value;
        }

        public class LockObect
        {
            public static List<LockObectMachine> List = new List<LockObectMachine>();

            public static LockObectMachine Get(int id)
            {
                lock (List)
                {
                    var item = List.FirstOrDefault(m => m.Id == id);

                    if (item == null)
                    {
                        item = new LockObectMachine { Id = id };
                        List.Add(item);
                    }

                    return item;
                }
            }
        }

        public class LockObectMachine
        {
            public int Id { get; set; }
        }

        /// <summary>
        ///     The expired time
        /// </summary>
        public TimeSpan ExpiredTime = new TimeSpan(0, -5, 0);

        /// <summary>
        ///     Конструктор репозитория сигналов измеряемых параметров. Работает с файлами, а не с объектами
        /// </summary>
        /// <param name="dataAccess">
        ///     Основной шлюз доступа
        /// </param>
        /// <param name="dataManager">
        ///     Менеджер данных
        /// </param>
        /// <param name="maximumObjects">
        ///     Максимальное количество объектов типа <see cref="AdditionalSignal" />
        /// </param>
        public AdditionalSignalRepository(string rootFolder)
        {
            RootFolder = rootFolder;
        }

        /// <summary>
        ///     Gets or sets the root folder.
        /// </summary>
        /// <value>
        ///     The root folder.
        /// </value>
        public string RootFolder { get; set; }

        /// <summary>
        ///     Gets the size of the page.
        /// </summary>
        /// <value>
        ///     The size of the page.
        /// </value>
        private static int PageSize { get { return 24 * 60 * 60 * 10; } }

        /// <summary>
        ///     Получить имя файла
        /// </summary>
        /// <param name="dateTime">Дата сигналов</param>
        /// <param name="machineId">Идентификатор станка, для которого получаем сигналы</param>
        /// <returns>
        ///     Имя файла
        /// </returns>
        private string GetFile(DateTime dateTime, int machineId)
        {
            var folder = Path.Combine(RootFolder, dateTime.Year.ToString());
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            folder = Path.Combine(folder, dateTime.Month.ToString());
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            folder = Path.Combine(folder, dateTime.Day.ToString());
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, machineId.ToString());
        }

        /// <summary>
        ///     Gets the filtered list.
        /// </summary>
        /// <param name="machineId">The machine identifier.</param>
        /// <param name="fromDate">From date.</param>
        /// <param name="toDate">To date.</param>
        /// <param name="machineParamId">The machine parameter identifier.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        ///     Не указан станок
        ///     or
        ///     Не найден ParamInMachine с machineParamId = " + machineParamId
        /// </exception>
        public List<AdditionalSignal> GetFilteredList(int machineId, DateTime fromDate, DateTime toDate, int machineParamId = -1, int pageSize = -1, ParamInMachineModel[] pimArr = null, bool isDiscardNaNValues = true)
        {
            if (pageSize == -1 && pimArr == null)
            {
                return GetFastList(machineId, fromDate, toDate, machineParamId)
                    .AsParallel()
                    .Select(row => new AdditionalSignal
                    {
                        MachineID = machineId,
                        ReceiveDate = new DateTime(2000, 1, 1).AddSeconds(row.DatetimeSeconds),
                        SignalValue = row.Value,
                        ParamInMachineID = row.ParamId
                    }).ToList();
            }

            toDate = toDate.AddMilliseconds(-1);

            var sw = new Stopwatch();
            var sw1 = new Stopwatch();
            sw.Start();
            int cntRes = -1;

            try
            {
                lock (LockObect.Get(machineId))
                {
                    sw1.Start();

                    var machine = DataManager.Machines.FirstOrDefault(m => m.Id == machineId);

                    if (machine == null)
                        throw new Exception("Not found machine");

                    if (machineParamId > 0)
                    {
                        var item =
                            machine.ParameterList.FirstOrDefault(pm => pm.MachineParamID == machineParamId);

                        if (item == null)
                            throw new Exception("Not found ParamInMachine with machineParamId = " + machineParamId);

                        // paramInMachineId = item.ID; 
                        pimArr = new ParamInMachineModel[] { item }; 
                    }

                    var files = GetFiles(machineId, fromDate, toDate);

                    //if (pageSize == -1)
                    //    pageSize = PageSize; // todo может умножить на размер записи

                    // todo: работа с листом
                    var signalRowList = new List<AdditionalSignalRow>(PageSize);

                    var fromDateTimeSeconds = (fromDate - new DateTime(2000, 1, 1)).TotalSeconds.ToInt(0);
                    var toDateTimeSeconds = (toDate - new DateTime(2000, 1, 1)).TotalSeconds.ToInt(0);

                    var allParamList = machine.ParameterList.ToList();

                    var dict = allParamList.ToDictionary(p => p.Id, p => p);

                    if (pimArr != null)
                        dict = pimArr.ToDictionary(p => p.Id, p => p);


                    foreach (var file in files)
                        using (var f = File.Open(file, FileMode.Open, FileAccess.Read))
                        {
                            if (pageSize > 0 && signalRowList.Count >= pageSize)
                                break;

                            var rowSize = sizeof(short) + sizeof(int) + sizeof(double);
                            var len = f.Length;
                            var cnt = len / rowSize;
                            var br = new BinaryReader(f);

                            for (var i = 0; i < cnt; i++)
                            {
                                if (pageSize > 0 && signalRowList.Count >= pageSize)
                                    break;

                                var row = new AdditionalSignalRow
                                {
                                    ParamId = br.ReadInt16(),
                                    DatetimeSeconds = br.ReadInt32(),
                                    Value = br.ReadDouble()
                                };

                                if (row.DatetimeSeconds < fromDateTimeSeconds)
                                    continue;

                                if (row.DatetimeSeconds > toDateTimeSeconds)
                                    continue;

                                if (isDiscardNaNValues && double.IsNaN(row.Value))
                                    continue;

                                if (!dict.ContainsKey(row.ParamId))
                                    continue;

                                signalRowList.Add(row);
                            }
                        }

                    cntRes = signalRowList.Count;

                    if (cntRes == 0)
                        return new List<AdditionalSignal>();

                    return (from row in signalRowList.AsParallel()
                            select new AdditionalSignal
                            {
                                MachineID = machineId,
                                ReceiveDate = new DateTime(2000, 1, 1).AddSeconds(row.DatetimeSeconds),
                                SignalValue = row.Value,
                                ParamInMachineID = row.ParamId
                            }).ToList();
                }
            }
            finally
            {
                sw.Stop();
                sw1.Stop();

                var tm = sw.ElapsedMilliseconds;
            }

        }

        /// <summary>
        ///     Gets the filtered list as AdditionalSignalRow
        /// </summary>
        /// <param name="machineId">The machine identifier.</param>
        /// <param name="fromDate">From date.</param>
        /// <param name="toDate">To date.</param>
        /// <param name="machineParamId">The machine parameter identifier.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        ///     Не указан станок
        ///     or
        ///     Не найден ParamInMachine с machineParamId = " + machineParamId
        /// </exception>
        public List<AdditionalSignalRow> GetFilteredListEx(MachineModel machine, DateTime fromDate, DateTime toDate, int machineParamId = -1, int pageSize = -1)
        {
            var machineId = machine.Id;
            var sw = new Stopwatch();
            var sw1 = new Stopwatch();
            sw.Start();
            var cntRes = 0;

            try
            {
                toDate = toDate.AddMilliseconds(-1);

                lock (LockObect.Get(machineId))
                {
                    sw1.Start();

                    if (machine == null)
                        throw new Exception("No select machine");

                    var paramInMachineId = 0;
                    if (machineParamId > 0)
                    {
                        var item = machine.ParameterList.FirstOrDefault(pm => pm.MachineParamID == machineParamId);

                        if (item == null)
                            throw new Exception("Not found ParamInMachine with machineParamId = " + machineParamId);

                        paramInMachineId = item.Id;
                    }

                    var files = GetFiles(machineId, fromDate, toDate);

                    //if (pageSize == -1)
                    //    pageSize = PageSize; // todo может умножить на размер записи

                    // todo: работа с листом
                    var signalRowList = new List<AdditionalSignalRow>(PageSize);

                    var fromDateTimeSeconds = (int)(fromDate - new DateTime(2000, 1, 1)).TotalSeconds;
                    var toDateTimeSeconds = (int)(toDate - new DateTime(2000, 1, 1)).TotalSeconds;

                    var allParamList = machine.ParameterList.ToList();

                    var dict = allParamList.ToDictionary(p => p.Id, p => p);

                    foreach (var file in files)
                        using (var f = File.Open(file, FileMode.Open, FileAccess.Read))
                        {
                            if (pageSize > 0 && signalRowList.Count >= pageSize)
                                break;

                            var rowSize = sizeof(short) + sizeof(int) + sizeof(double);
                            var len = f.Length;
                            var cnt = len / rowSize;
                            var br = new BinaryReader(f);

                            // byte[] bts1 = new byte[(int)len];
                            // br.Read(bts1, 0, (int)len);

                            for (var i = 0; i < cnt; i++)
                            {
                                if (pageSize > 0 && signalRowList.Count >= pageSize)
                                    break;

                                // byte[] bts = new byte[rowSize];
                                // br.Read(bts, 0, rowSize);

                                var row = new AdditionalSignalRow
                                {
                                    ParamId = br.ReadInt16(),
                                    DatetimeSeconds = br.ReadInt32(),
                                    Value = br.ReadDouble()
                                };

                                if (row.DatetimeSeconds < fromDateTimeSeconds)
                                    continue;

                                if (row.DatetimeSeconds > toDateTimeSeconds)
                                    continue;

                                if (/*isDiscardNaNValues && */double.IsNaN(row.Value))
                                    continue;

                                if (machineParamId != -1
                                    && paramInMachineId != 0)
                                    if (row.ParamId != paramInMachineId)
                                        continue;

                                if (!dict.ContainsKey(row.ParamId))
                                    continue;

                                signalRowList.Add(row);
                                cntRes = signalRowList.Count;
                            }
                        }

                    return signalRowList.ToList();
                }
            }
            finally
            {
                sw.Stop();

                var tm = sw.ElapsedMilliseconds;
            }
        }

        public class ParamStatData
        {
            public int Id;
            public double Sum = 0;
            public double Max = double.MinValue;
            public double Min = double.MaxValue;
            public int Count = 0;
            public double ValueSeconds = 0;

            private int? beforeTime;

            public ParamStatData(int id)
            {
                this.Id = id;
            }

            public void Do(int timeInSecs, double val)
            {
                if (!this.beforeTime.HasValue)
                    this.beforeTime = timeInSecs;

                this.Count++;
                this.Sum += val;

                if (this.Max < val)
                    this.Max = val;

                if (this.Min > val)
                    this.Min = val;

                var scnds = timeInSecs - this.beforeTime.Value;
                this.beforeTime = timeInSecs;

                this.ValueSeconds += val * scnds;
            }
        }

        public Dictionary<int, ParamStatData> GetParamsStatData(MachineModel machine, DateTime fromDate, DateTime toDate, int machineParamId = -1)
        {
            try
            {
                var fromDateTimeSeconds = (int)(fromDate - new DateTime(2000, 1, 1)).TotalSeconds;
                var toDateTimeSeconds = (int)(toDate - new DateTime(2000, 1, 1)).TotalSeconds;

                var allParamList = machine.ParameterList.ToList();

                var dict = allParamList.ToDictionary(p => p.Id, p => new ParamStatData(p.Id));

                var machineId = machine.Id;
                var sw = new Stopwatch();
                var sw1 = new Stopwatch();
                sw.Start();
                var cntRes = 0;

                try
                {
                    toDate = toDate.AddMilliseconds(-1);

                    lock (LockObect.Get(machineId))
                    {
                        sw1.Start();

                        if (machine == null)
                            throw new Exception("No machine specified");

                        var paramInMachineId = 0;

                        if (machineParamId > 0)
                        {
                            var item = machine.ParameterList.FirstOrDefault(pm => pm.MachineParamID == machineParamId);
                            if (item == null)
                                throw new Exception("ParamInMachine not found with machineParamId =" + machineParamId);

                            paramInMachineId = item.Id;
                        }

                        var files = GetFiles(machineId, fromDate, toDate);

                        //if (pageSize == -1)
                        //    pageSize = PageSize; // todo может умножить на размер записи

                        // todo: работа с листом


                        foreach (var file in files)
                            using (var f = File.Open(file, FileMode.Open, FileAccess.Read))
                            {
                                var rowSize = sizeof(short) + sizeof(int) + sizeof(double);
                                var len = f.Length;
                                var cnt = len / rowSize;
                                var br = new BinaryReader(f);

                                for (var i = 0; i < cnt; i++)
                                {
                                    var paramId = br.ReadInt16();
                                    var datetimeSeconds = br.ReadInt32();
                                    var value = br.ReadDouble();

                                    if (datetimeSeconds < fromDateTimeSeconds)
                                        continue;

                                    if (datetimeSeconds > toDateTimeSeconds)
                                        continue;

                                    if (machineParamId != -1
                                        && paramInMachineId != 0)
                                        if (paramId != paramInMachineId)
                                            continue;


                                    if (!dict.ContainsKey(paramId))
                                        continue;

                                    var data = dict[paramId];

                                    data.Do(datetimeSeconds, value);
                                }
                            }

                        return dict;
                    }
                }
                finally
                {
                    sw.Stop();

                    var tm = sw.ElapsedMilliseconds;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<AdditionalSignalRow> GetFastList(int machineId, DateTime fromDate, DateTime toDate, int machineParamId = -1)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var cntRes = 0;

                try
                {
                    toDate = toDate.AddMilliseconds(-1);

                    lock (LockObect.Get(machineId))
                    {
                        var re = new List<AdditionalSignalRow>();

                        int fromDateTimeSeconds = (int)(fromDate - new DateTime(2000, 1, 1)).TotalSeconds;
                        int toDateTimeSeconds = (int)(toDate - new DateTime(2000, 1, 1)).TotalSeconds;

                        var machine = DataManager.Machines.FirstOrDefault(m => m.Id == machineId);
                        var allParamList = machine.ParameterList.ToList();

                        if (machineParamId != -1)
                            allParamList.RemoveAll(p => p.MachineParamID != machineParamId);

                        if (!allParamList.Any())
                            return new List<AdditionalSignalRow>();

                        var dict = allParamList.ToDictionary(p => p.Id, p => p);

                        var files = GetFiles(machineId, fromDate, toDate);

                        return ReadData(fromDateTimeSeconds, toDateTimeSeconds, dict, files).ToList();
                    }
                }
                finally
                {
                    sw.Stop();

                    var tm = sw.ElapsedMilliseconds;
                }
            }
            catch (Exception ex)
            {
                return new List<AdditionalSignalRow>();
            }
        }

        private static IEnumerable<AdditionalSignalRow> ReadData(int fromDateTimeSeconds, int toDateTimeSeconds, Dictionary<int, ParamInMachineModel> dict, List<string> files)
        {
            var idSize = sizeof(short); // Размер идентификатора - 2 байта
            var dtSize = sizeof(int); // Размер даты - 4 байта
            var valSize = sizeof(double); // Размер значения - 8 байт

            var fullSize = idSize + dtSize + valSize; // Полный размер одного одной записи - 14 байт
            var itemCnt = 100; // Количество записей, читаемых за раз
            var partSize = fullSize * itemCnt; // Максимальный размер, читаемый за одну операцию

            foreach (var file in files)
            {
                using (var f = File.OpenRead(file))
                using (var br = new BinaryReader(f))
                {
                    while (f.Position < f.Length) // Пока можно читать
                    {
                        var ba = br.ReadBytes(partSize);

                        for (var i = 0; i < ba.Length; i = i + fullSize)
                        {
                            // Читать можем в любом порядке. Сначала лучше проверить дату, а уже потом идентификатор параметра
                            var dt = BitConverter.ToInt32(ba, i + idSize);

                            if (dt < fromDateTimeSeconds || dt > toDateTimeSeconds)
                                continue;

                            var paramId = BitConverter.ToInt16(ba, i);

                            if (!dict.ContainsKey(paramId))
                                continue;

                            yield return new AdditionalSignalRow
                            {
                                ParamId = paramId,
                                DatetimeSeconds = dt,
                                Value = BitConverter.ToDouble(ba, i + idSize + dtSize)
                            };
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the files.
        /// </summary>
        /// <param name="machineId">The machine identifier.</param>
        /// <param name="fromDate">From date.</param>
        /// <param name="toDate">To date.</param>
        /// <returns></returns>
        private List<string> GetFiles(int machineId, DateTime fromDate, DateTime toDate)
        {
            var files = new List<string>();
            var years = Directory.GetDirectories(RootFolder);
            var yearFolderList = GetFolderNameList(years);

            foreach (var year in yearFolderList.OrderBy(y => y))
            {
                if ((int)year.ToInt(0) < fromDate.Year)
                    continue;
                if ((int)year.ToInt(0) > toDate.Year)
                    continue;

                var months = Directory.GetDirectories(Path.Combine(RootFolder, year));
                var monthFolderList = GetFolderNameList(months);

                foreach (var m in monthFolderList.OrderBy(mm => mm))
                {
                    if (year.ToInt(0) == fromDate.Year
                        && m.ToInt(0) < fromDate.Month)
                        continue;
                    if (year.ToInt(0) == toDate.Year
                        && m.ToInt(0) > toDate.Month)
                        continue;

                    var days = Directory.GetDirectories(Path.Combine(RootFolder, year, m));
                    var dayFolderList = GetFolderNameList(days);
                    foreach (var d in dayFolderList.OrderBy(dd => dd))
                    {
                        if (year.ToInt(0) == fromDate.Year
                            && m.ToInt(0) == fromDate.Month
                            && d.ToInt(0) < fromDate.Day)
                            continue;
                        if (year.ToInt(0) == toDate.Year
                            && m.ToInt(0) == toDate.Month
                            && d.ToInt(0) > toDate.Day)
                            continue;
                        var file = Path.Combine(RootFolder, year, m, d, machineId.ToString());
                        if (File.Exists(file))
                            files.Add(file);
                    }
                }
            }
            return files;
        }

        private static IEnumerable<string> GetFolderNameList(string[] days)
        {
            var dayFolderList = from d in days
                                select new DirectoryInfo(d).Name;

            return dayFolderList;
        }
    }

    public static class Tools
    {
        public static int ToInt(this object val, int defValue = 0)
        {
            if (val == null)
                return defValue;

            return Convert.ToInt32(val);
        }

    }
}
