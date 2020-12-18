using DevExpress.XtraCharts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TestIzm;
using TestIzm.Bl;
using TestIzm.Model;
using static TestIzm.AdditionalSignalRepository;

namespace WindowsFormsApp1
{
    public partial class TechnologyControl : Form
    {
        AnalizeTool AnalizeTool { get; set; }
        public ConstantLine MeenTimeLine { get; private set; }
        public ConstantLine UCLTimeLine { get; private set; }
        public ConstantLine LCLTimeLine { get; private set; }
        public ConstantLine Meenline { get; private set; }
        public ConstantLine UCLline { get; private set; }
        public ConstantLine LCLline { get; private set; }
        public ConstantLine UCLWarningline { get; private set; }
        public ConstantLine LCLWarningline { get; private set; }

        private MachineModel _selectedMachine => this.lueMachine.GetSelectedDataRow() as MachineModel;
        private RowX _selectedFirstMachineParam => this.lueFirstMachineParam.GetSelectedDataRow() as RowX;
        private RowX _selectedSecondMachineParam => this.lueSecondMachineParam.GetSelectedDataRow() as RowX;
        private SwiftPlotDiagram _diagram => this.ccMain.Diagram as SwiftPlotDiagram;

        private List<AdditionalSignalRow> _currentSignalList;

        #region Ctor
        public TechnologyControl()
        {
            InitializeComponent();

            this.ccAnalysis.ConstantLineMoved += ccAnalysis_ConstantLineMoved;
            this.ccAnalysis.MouseUp += ccAnalysisl_MouseUp;
            this.ccAnalysis.CustomDrawCrosshair += ccAnalysis_CustomDrawCrosshair;
        }

        private void TechnologyControl_Load(object sender, EventArgs e)
        {
            this.PrepareUIElements();

            this.deFrom.DateTime = DateTime.Now.Date;
            this.deTo.DateTime = DateTime.Now.Date.AddDays(-1);
            this.teFrom.EditValue = TimeSpan.Zero;
            this.teTo.EditValue = DateTime.Now.TimeOfDay;

            this.AnalizeTool = new AnalizeTool(new TestIzm.Properties.Settings().UrlFiles);

            this.lueMachine.Properties.DataSource = DataManager.Machines;
            this.lueMachine.ItemIndex = 0;
        }

        private void PrepareUIElements()
        {
            teMin.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            teMin.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Default;

            teMax.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            teMax.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Default;

            teMaxCount.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            teMaxCount.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Default;

            teMinCount.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            teMinCount.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Default;

            teNorm.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            teNorm.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Default;
        }

        #endregion

        private void sbLoad_Click(object sender, EventArgs e)
        {
            this.LoadData();
        }

        private void lueMachine_EditValueChanged(object sender, EventArgs e)
        {
            this.FillMachineParam();
        }

        private void lueFirstMachineParam_EditValueChanged(object sender, EventArgs e)
        {
            if (this._selectedFirstMachineParam is null || this._selectedFirstMachineParam.Id == 0)
                return;

            this.AnalizeTool.SetMainParam(this._selectedFirstMachineParam.Id);
        }

        private void AnalysisChanged(object sender, EventArgs e)
        {
            AddSeriesComparerChart(this._analisis);
        }

        private void FillMachineParam()
        {
            var notSelectedItem = this.GetNotSelectedItem();
            var rowXList = new List<RowX>() { notSelectedItem };

            if (this._selectedMachine is null)
            {
                this.lueFirstMachineParam.Properties.DataSource = rowXList;
                this.lueFirstMachineParam.EditValue = notSelectedItem;

                this.lueSecondMachineParam.Properties.DataSource = rowXList;
                this.lueSecondMachineParam.EditValue = notSelectedItem;
                return;
            }

            var selectedFirstMachineParam = this._selectedFirstMachineParam;
            var selectedSecondMachineParam = this._selectedSecondMachineParam;

            RowX setForSelectedFirst = null;
            RowX setForSelectedSecond = null;

            foreach (var paramInMachine in this._selectedMachine.ParameterList)
            {
                var rowX = new RowX
                {
                    Id = paramInMachine.Id,
                    MachineParamId = paramInMachine.MachineParamID,
                    Name = paramInMachine.Name,
                    SignalList = AnalizeTool.GetTelemetry(paramInMachine.Id)
                };

                if (selectedFirstMachineParam?.MachineParamId == paramInMachine.MachineParamID)
                    setForSelectedFirst = rowX;

                if (selectedSecondMachineParam?.MachineParamId == paramInMachine.MachineParamID)
                    setForSelectedSecond = rowX;

                rowXList.Add(rowX);
            }

            this.lueFirstMachineParam.Properties.DataSource = rowXList;
            this.lueFirstMachineParam.EditValue = setForSelectedFirst ?? rowXList.FirstOrDefault();

            this.lueSecondMachineParam.Properties.DataSource = rowXList;
            this.lueSecondMachineParam.EditValue = setForSelectedSecond ?? rowXList.FirstOrDefault();
        }

        private RowX GetNotSelectedItem() => new RowX { Id = 0, MachineParamId = 0, Name = "Не выбрано", SignalList = new List<AdditionalSignalRow>() };

        private void LoadData()
        {
            if (this._selectedMachine is null)
                return;

            var dtFrom = this.deFrom.DateTime.Date.Add(this.teFrom.Time.TimeOfDay);
            var dtTo = this.deTo.DateTime.Date.Add(this.teTo.Time.TimeOfDay);

            AnalizeTool.Load(dtFrom, dtTo, this._selectedMachine);
            AddSeriesToMainChart();
            //BuildChartPie();
        }

        private SeriesPoint AddSeriesPointChart(UpModel up, int index, double value)
        {
            var sp = new SeriesPoint("CNC " + index, value);
            sp.Tag = up;
            sp.Color = Color.LightGray;

            if (up.UpType == UpModel.UpTypeEnum.Good)
                sp.Color = Color.LightGreen;

            if (up.UpType == UpModel.UpTypeEnum.Warning)
                sp.Color = Color.Yellow;

            if (up.UpType == UpModel.UpTypeEnum.Error)
                sp.Color = Color.LightPink;

            return sp;
        }

        private double GetValue(UpModel a, int paramId, int valueTypeId)
        {
            var mark = a.GetMark(paramId);

            if (valueTypeId == 0)
                return mark.Avg;

            if (valueTypeId == 1)
                return mark.AvgDiff;

            if (valueTypeId == 2)
                return mark.Disp;

            if (valueTypeId == 3)
                return a.Time;

            return mark.Avg;
        }

        // здесь обработка УП ТО и заготовок
        private void RepaintCncLogColors()
        {
            var analisis = _analisis;

            if (analisis == null)
                analisis = this.AnalizeTool.GetAnalisis(lastMouseDatetime);

            if (analisis == null)
                return;

            if (this._selectedFirstMachineParam is null || this._selectedFirstMachineParam.Id == 0)
                return;

            if (this._diagram is null)
                return;

            var diagram = this._diagram;
            diagram.AxisX.Strips.Clear();

            analisis.ColorizeMost(this._selectedFirstMachineParam.Id);

            foreach (var upModel in analisis.UpModelList)
            {
                var strip = new Strip("CNC " + upModel.Index, upModel.Up.DtStart, upModel.Up.DtEnd) { Color = Color.LightGray };

                if (upModel.IsMost)
                {
                    strip.Color = Color.LightGreen;

                    if (upModel.UpType == UpModel.UpTypeEnum.Warning)
                        strip.Color = Color.Yellow;

                    if (upModel.UpType == UpModel.UpTypeEnum.Error)
                        strip.Color = Color.LightPink;
                }

                if (upModel == _selectedUp)
                    strip.FillStyle.FillMode = DevExpress.XtraCharts.FillMode.Hatch;

                diagram.AxisX.Strips.Add(strip);
            }

            AddSeriesToAnalysisChart(analisis);
        }

        #region Unused

        bool isShowAll = true;

        private void sbHideAll_Click(object sender, EventArgs e)
        {
            this.isShowAll = !this.isShowAll;

            this.lcgLevels.Visibility = this.isShowAll ? DevExpress.XtraLayout.Utils.LayoutVisibility.Always : DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
            this.lcgOtcl.Visibility = this.isShowAll ? DevExpress.XtraLayout.Utils.LayoutVisibility.Always : DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
        }

        private void BuildChartPie()
        {
            this.chartPie.Series.Clear();
            chartPie.Titles.Clear();

            try
            {
                var series1 = new Series("Land Area by Country", ViewType.Pie);

                series1.DataSource = DataPointChartPie.GetDataPoints();
                series1.ArgumentDataMember = "Argument";
                series1.ValueDataMembers.AddRange(new string[] { "Value" });

                chartPie.Series.Add(series1);

                series1.Label.TextPattern = "{VP:p0} ({V:.##})";

                chartPie.Titles.Add(new ChartTitle());
                chartPie.Titles[0].Text = "Warnings";

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public class DataPointChartPie
        {
            public string Argument { get; set; }
            public double Value { get; set; }

            public static List<DataPointChartPie> GetDataPoints()
            {
                return new List<DataPointChartPie>
                {
                    new DataPointChartPie { Argument = "Critical",    Value = 4.2},
                    new DataPointChartPie { Argument = "Warnings",    Value = 16.3},
                    new DataPointChartPie { Argument = "Ok",    Value = 79.5},
                };
            }
        }

        #endregion

        #region Main Chart

        private DateTime lastMouseDatetime;
        private bool _isNeedZoom = false;

        // здесь обработка УП ТО и заготовок
        private void ccMain_CustomDrawCrosshair(object sender, CustomDrawCrosshairEventArgs e)
        {
            if (e.CrosshairElementGroups.Count == 0 || this._selectedFirstMachineParam is null)
                return;

            var group = e.CrosshairElementGroups.First();
            this.lastMouseDatetime = group.CrosshairElements[0].SeriesPoint.DateTimeArgument;

            var upModel = this.AnalizeTool.GetUpModel(this.lastMouseDatetime);

            var state = "idle";
            var upName = "-";
            var upInfo = "-";

            if (upModel == null)
                state = "-";
            else
            {
                // this.ups = DataManager.CncLog.Where(cncLog => cncLog.Machine.ID == AnalizeTool.Machine.Id && cncLog.NameUP == up.NameUP && cncLog.DtStart >= up.DtStart.AddDays(-1) && cncLog.DtStart <= up.DtStart.AddDays(1)).ToList();

                upName = upModel.Up.NameUP + " - " + upModel.Index;
                upInfo = "Time: " + new TimeSpan(0, 0, 0, 0, upModel.Up.ProcessingTime.ToInt(0)).ToString() + Environment.NewLine
                    + "F%: " + upModel.Up.AvgPercentCorrectFeed?.ToString("0.0") + Environment.NewLine
                    + "S%: " + upModel.Up.AvgPercentCorrectSpeed?.ToString("0.0") + Environment.NewLine;

                var mark = upModel.GetMark(this._selectedFirstMachineParam.Id);

                if (mark != null)
                {
                    upInfo += this._selectedFirstMachineParam.Name + Environment.NewLine + " Avg: " + mark.Avg.ToString("0.00") + Environment.NewLine;
                    upInfo += " Avg diff: " + mark.AvgDiff.ToString("0.00") + Environment.NewLine;
                    upInfo += " Dispersion: " + mark.Disp.ToString("0.00") + Environment.NewLine;
                }
                else
                {
                    upInfo += " no mark: " + this._selectedFirstMachineParam.Name;
                }

                if (upModel.Up.DtEnd > lastMouseDatetime)
                    state = "running";
            }

            if (group.CrosshairElements[0] != null)
                group.HeaderElement.Text = String.Format("Cnc: {1} State: {2} {3}{4}Datetime: {0:yyyy.MM.dd HH:mm:ss}", lastMouseDatetime, upName, state, Environment.NewLine, upInfo);

        }

        private void ccMain_Resize(object sender, EventArgs e)
        {
            _isNeedZoom = true;
        }

        // здесь обработка УП ТО и заготовок
        private void ccMain_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (this._diagram != null)
                    this._diagram.AxisX.Strips.Clear();

                this._selectedUp = null;
            }
        }

        // здесь обработка УП ТО и заготовок
        private void ccMain_DoubleClick(object sender, EventArgs e)
        {
            this._analisis = null;
            RepaintCncLogColors();
        }

        private void ccMain_Zoom(object sender, ChartZoomEventArgs e)
        {
            _isNeedZoom = true; ;
        }

        private void AutoZoomY()
        {
            if (this._diagram is null)
                return;

            var diagram = this._diagram;
            var lst = this._currentSignalList?.Where(a => a.Datetime >= (DateTime)diagram.AxisX.VisualRange.MinValue && a.Datetime <= (DateTime)diagram.AxisX.VisualRange.MaxValue);
            var lstIsEmpty = lst is null || lst.Count() == 0;
            var minY = !lstIsEmpty ? lst.Min(a => a.Value) * 0.9 : 0;
            var maxY = !lstIsEmpty ? lst.Max(a => a.Value) * 1.11 : 0;

            diagram.AxisY.VisualRange.MinValue = minY;
            diagram.AxisY.VisualRange.MaxValue = maxY;

            if (this._selectedSecondMachineParam != null)
            {
                lst = this._selectedSecondMachineParam.SignalList?.Where(a => a.Datetime >= (DateTime)diagram.AxisX.VisualRange.MinValue && a.Datetime <= (DateTime)diagram.AxisX.VisualRange.MaxValue);
                lstIsEmpty = lst is null || lst.Count() == 0;
                minY = !lstIsEmpty ? lst.Min(a => a.Value) * 0.9 : 0;
                maxY = !lstIsEmpty ? lst.Max(a => a.Value) * 1.11 : 0;

                var axis = (ccMain.Series[1]?.View as SwiftPlotSeriesView)?.AxisY;
                if (axis is null)
                    return;

                axis.VisualRange.MinValue = minY;
                axis.VisualRange.MaxValue = maxY;
            }
        }

        private void ccMain_Scroll(object sender, ChartScrollEventArgs e)
        {
            this._isNeedZoom = true;
        }

        private void tmAutoZoom_Tick(object sender, EventArgs e)
        {
            if (this._isNeedZoom)
                AutoZoomY();

            this._isNeedZoom = false;

            if (this._isRepaintCncLogColors)
            {
                this._isRepaintCncLogColors = false;
                RepaintCncLogColors();
            }

            this.tmAutoZoom.Interval = 500;
        }

        // здесь обработка УП ТО и заготовок
        void AddSeriesToMainChart()
        {
            this.ccMain.Series.Clear();

            if (this._selectedFirstMachineParam is null || this._selectedFirstMachineParam.Id == 0)
            {
                this._currentSignalList = new List<AdditionalSignalRow>();
                return;
            }

            this._currentSignalList = this._selectedFirstMachineParam.SignalList ?? new List<AdditionalSignalRow>();

            this.ccMain.BeginInit();
            var series = new Series();
            var view = new SwiftPlotSeriesView();
            series.View = view;

            series.Points.AddRange(this._currentSignalList.Select(a => new SeriesPoint(new DateTime(2000, 1, 1)
                .AddSeconds(a.DatetimeSeconds), a.Value)).ToArray());

            this.ccMain.Series.Add(series);
            this.ccMain.EndInit();

            if (this._diagram is null)
                return;

            var diagram = this._diagram;
            diagram.AxisX.ConstantLines.Clear();

            //здесь обработка ТО и УП и заготовок
            foreach (var seriesAnalisis in this.AnalizeTool.UpSeriesAnalisisList)
            {
                var line = new ConstantLine(seriesAnalisis.UpName, seriesAnalisis.DtFrom) { ShowInLegend = false };
                line.Title.Text = seriesAnalisis.UpName;
                line.Title.Visible = true;

                diagram.AxisX.ConstantLines.Add(line);
            }

            if (diagram.Panes.Count > 0)
                diagram.Panes.Clear();

            if (diagram.SecondaryAxesY.Count > 0)
                diagram.SecondaryAxesY.Clear();

            if (this._selectedSecondMachineParam != null && this._selectedSecondMachineParam.SignalList?.Count > 0)
            {
                var pane = new XYDiagramPane("Next");
                diagram.Panes.Add(pane);

                series = new Series();
                var view2 = new SwiftPlotSeriesView();
                series.View = view2;
                series.Points.AddRange(this._selectedSecondMachineParam.SignalList.Select(a => new SeriesPoint(new DateTime(2000, 1, 1).AddSeconds(a.DatetimeSeconds), a.Value)).ToArray());
                view2.Pane = pane;

                ccMain.CrosshairOptions.ShowOnlyInFocusedPane = false;
                ccMain.Series.Add(series);

                diagram.SecondaryAxesY.Add(new SwiftPlotDiagramSecondaryAxisY("My Axis Y"));
                diagram.SecondaryAxesY[0].Alignment = AxisAlignment.Near;

                view2.AxisY = diagram.SecondaryAxesY[0];
            }

            diagram.EnableAxisXZooming = true;
            diagram.EnableAxisXScrolling = true;
            diagram.AxisY.VisualRange.Auto = true;

            diagram.AxisY.Strips.Clear();
            this._isNeedZoom = true;
        }

        #endregion

        #region Analysis Chart

        //обработка УП ТО и заготовок
        private UpModel _selectedUp;
        UpSeriesAnalisis _analisis;

        private bool _isRepaintCncLogColors = false;
        private bool _isChartDetailConstantLineMoved = false;
        private bool _isTimeConstantLineMoved = false;
        private void ccAnalysis_CustomDrawCrosshair(object sender, CustomDrawCrosshairEventArgs e)
        {
            if (e.CrosshairElementGroups.Count == 0)
                return;

            var upModel = e.CrosshairElementGroups.FirstOrDefault()?.CrosshairElements.FirstOrDefault()?.SeriesPoint.Tag as UpModel;
            if (upModel is null)
                return;

            if (this._selectedUp != null && this._selectedUp == upModel)
                return;

            this._selectedUp = upModel;
            this._isRepaintCncLogColors = true;

            this.tmAutoZoom.Interval = 50;
        }

        //обработка УП ТО и заготовок
        private void ccAnalysisl_MouseUp(object sender, MouseEventArgs e)
        {
            if (this._analisis is null)
                return;

            if (this._isChartDetailConstantLineMoved)
            {
                this._isChartDetailConstantLineMoved = false;
                this._analisis.UCLTime = (double)UCLTimeLine.AxisValue;
                this._analisis.LCLTime = (double)LCLTimeLine.AxisValue;
                this._analisis.MostAvgTime = (double)MeenTimeLine.AxisValue;

                this._analisis.UCL = (double)UCLline.AxisValue;
                this._analisis.LCL = (double)LCLline.AxisValue;

                this._analisis.UCLWarning = (double)UCLWarningline.AxisValue;
                this._analisis.LCLWarning = (double)LCLWarningline.AxisValue;

                this._analisis.Avg = (double)Meenline.AxisValue;

                this._analisis.RecalcMosted();

                if (_isTimeConstantLineMoved)
                {
                    _isTimeConstantLineMoved = false;
                    this._analisis.RecalcLevels();
                }

                this._analisis.ColorizeMost();

                this.RepaintCncLogColors();
                this.AddSeriesToAnalysisChart(this._analisis);
            }
        }

        private void ccAnalysis_ConstantLineMoved(object sender, ConstantLineMovedEventArgs e)
        {
            this._isChartDetailConstantLineMoved = true;

            if (e.ConstantLine == UCLTimeLine || e.ConstantLine == LCLTimeLine)
            {
                this._isTimeConstantLineMoved = true;
            }
        }

        //обработка УП ТО и заготовок
        void AddSeriesToAnalysisChart(UpSeriesAnalisis analisis)
        {
            if (this._selectedFirstMachineParam is null || this._selectedFirstMachineParam.Id == 0)
                return;

            _analisis = analisis;
            var chart = ccAnalysis;
            chart.BeginInit();
            chart.Series.Clear();

            var seriesAvg = new Series("Avg", ViewType.Bar);
            var seriesTime = new Series("Time", ViewType.Line);

            var valueTypeId = ceAvg.SelectedIndex;

            if (analisis == null)
                return;

            var list = analisis.UpModelList.ToList();

            if (this.ceIsHideFalseData.Checked)
                list = list.Where(m => m.UpType != UpModel.UpTypeEnum.Other).ToList();

            seriesAvg.Points.AddRange(list.Select(a => AddSeriesPointChart(a, a.Index, GetValue(a, this._selectedFirstMachineParam.Id, valueTypeId))).ToArray());
            chart.Series.Add(seriesAvg);

            seriesTime.Points.AddRange(list.Select(a => AddSeriesPointChart(a, a.Index, a.Time)).ToArray());
            chart.Series.Add(seriesTime);

            chart.EndInit();

            ((XYDiagram)chart.Diagram).SecondaryAxesY.Clear();

            var myAxisY = new SecondaryAxisY("Time");
            ((XYDiagram)chart.Diagram).SecondaryAxesY.Add(myAxisY);
            ((LineSeriesView)seriesTime.View).AxisY = myAxisY;
            myAxisY.ConstantLines.Clear();

            this.MeenTimeLine = new ConstantLine("Meen time", analisis.MostAvgTime);
            this.MeenTimeLine.ShowInLegend = false;
            this.MeenTimeLine.RuntimeMoving = true;
            this.MeenTimeLine.Color = Color.DarkOliveGreen;
            this.MeenTimeLine.LineStyle.DashStyle = DashStyle.Dot;
            myAxisY.ConstantLines.Add(this.MeenTimeLine);

            this.UCLTimeLine = new ConstantLine("UCL time", analisis.UCLTime);
            this.UCLTimeLine.ShowInLegend = false;
            this.UCLTimeLine.RuntimeMoving = true;
            this.UCLTimeLine.Color = Color.DarkRed;
            this.UCLTimeLine.LineStyle.DashStyle = DashStyle.DashDot;

            myAxisY.ConstantLines.Add(this.UCLTimeLine);

            this.LCLTimeLine = new ConstantLine("LCL time", analisis.LCLTime);
            this.LCLTimeLine.ShowInLegend = false;
            this.LCLTimeLine.RuntimeMoving = true;
            this.LCLTimeLine.Color = Color.DarkRed;
            this.LCLTimeLine.LineStyle.DashStyle = DashStyle.DashDot;
            myAxisY.ConstantLines.Add(this.LCLTimeLine);

            var axisY = ((XYDiagram)chart.Diagram).AxisY;
            axisY.ConstantLines.Clear();
            // ((LineSeriesView)seriesTime.View)
            this.Meenline = new ConstantLine("Meen", analisis.Avg);
            this.Meenline.ShowInLegend = false;
            this.Meenline.RuntimeMoving = true;
            this.Meenline.Color = Color.DarkOliveGreen;
            this.Meenline.LineStyle.DashStyle = DashStyle.Dot;

            axisY.ConstantLines.Add(this.Meenline);

            this.UCLline = new ConstantLine("UCL", analisis.UCL);
            this.UCLline.ShowInLegend = false;
            this.UCLline.RuntimeMoving = true;
            this.UCLline.Color = Color.Red;
            this.UCLline.LineStyle.DashStyle = DashStyle.Dash;
            axisY.ConstantLines.Add(this.UCLline);

            this.LCLline = new ConstantLine("LCL", analisis.LCL);
            this.LCLline.ShowInLegend = false;
            this.LCLline.RuntimeMoving = true;
            this.LCLline.Color = Color.Red;
            this.LCLline.LineStyle.DashStyle = DashStyle.Dash;
            axisY.ConstantLines.Add(this.LCLline);

            this.UCLWarningline = new ConstantLine("UCL Warning", analisis.UCLWarning);
            this.UCLWarningline.ShowInLegend = false;
            this.UCLWarningline.RuntimeMoving = true;
            this.UCLWarningline.Color = Color.DarkOrange;
            this.UCLWarningline.LineStyle.DashStyle = DashStyle.DashDotDot;
            axisY.ConstantLines.Add(this.UCLWarningline);

            this.LCLWarningline = new ConstantLine("LCL Warning", analisis.LCLWarning);
            this.LCLWarningline.ShowInLegend = false;
            this.LCLWarningline.RuntimeMoving = true;
            this.LCLWarningline.Color = Color.DarkOrange;
            this.LCLWarningline.LineStyle.DashStyle = DashStyle.DashDotDot;
            axisY.ConstantLines.Add(this.LCLWarningline);

            if (ccbeAdditionalParam.Properties.Items[1].CheckState != CheckState.Checked)
            {
                this.Meenline.Visible = false;
                this.UCLline.Visible = false;
                this.LCLline.Visible = false;
                this.UCLWarningline.Visible = false;
                this.LCLWarningline.Visible = false;
            }

            if (ccbeAdditionalParam.Properties.Items[0].CheckState != CheckState.Checked)
            {
                this.MeenTimeLine.Visible = false;
                this.UCLTimeLine.Visible = false;
                this.LCLTimeLine.Visible = false;
            }
        }

        //обработка УП ТО и заготовок
        private void ccAnalysis_SelectedItemsChanged(object sender, SelectedItemsChangedEventArgs e)
        {
            this.Text = e.NewItems.Count.ToString();
            var diagram = ccMain.Diagram as XYDiagram;
            if (diagram is null)
                return;

            foreach (SeriesPoint item in e.NewItems)
            {
                var up = item.Tag as UpModel;
                if (up is null)
                    continue;

                var constantLine1 = new ConstantLine("CNC >" + up.Index);
                diagram.AxisY.ConstantLines.Add(constantLine1);
                constantLine1.AxisValue = up.Up.DtStart;

                constantLine1.ShowInLegend = false;

                constantLine1.Color = Color.DarkGray;
                constantLine1.LineStyle.DashStyle = DashStyle.Dash;
                constantLine1.LineStyle.Thickness = 2;
            }
        }

        //обработка УП ТО и заготовок
        private void ccAnalysis_MouseLeave(object sender, EventArgs e)
        {
            this._selectedUp = null;
            this._isRepaintCncLogColors = true;
        }

        #endregion

        #region Comparer Chart

        void AddSeriesComparerChart(UpSeriesAnalisis analisis)
        {
            if (this._selectedFirstMachineParam is null || this._selectedFirstMachineParam.Id == 0)
                return;

            _analisis = analisis;
            var chart = ccAnalysis;
            chart.BeginInit();
            chart.Series.Clear();

            // var seriesAvg = new Series("Avg", ViewType.Bar);
            var seriesTime = new Series("Time", ViewType.Bar);

            // Вообще тут хорошо бы иметь кластеризацию
            // разделим Time на 10 частей
            // var min = 

            var valueTypeId = ceAvg.SelectedIndex;

            if (analisis == null)
                return;

            var list = analisis.UpModelList.ToList();

            if (this.ceIsHideFalseData.Checked)
                list = list.Where(m => m.UpType != UpModel.UpTypeEnum.Other).ToList();

            // seriesAvg.Points.AddRange(list.Select(a => AddSeriesPointChart(a, a.Index, GetValue(a, rowX.Id, valueTypeId))).ToArray());
            // chart.Series.Add(seriesAvg);
            // 
            // seriesTime.Points.AddRange(list.Select(a => AddSeriesPointChart(a, a.Index, a.Time)).ToArray());
            // chart.Series.Add(seriesTime);
            // 
            // chart.EndInit();
            // 
            // ((XYDiagram)chart.Diagram).SecondaryAxesY.Clear();
            // 
            // var myAxisY = new SecondaryAxisY("Time");
            // ((XYDiagram)chart.Diagram).SecondaryAxesY.Add(myAxisY);
            // ((LineSeriesView)seriesTime.View).AxisY = myAxisY;
        }

        #endregion
    }
}
