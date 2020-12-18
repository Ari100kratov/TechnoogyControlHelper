using DevExpress.XtraCharts;
using DevExpress.XtraCharts.Native;
using DevExpress.XtraExport.Implementation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using TestIzm;
using TestIzm.Bl;
using TestIzm.Model;
using TestIzm.Model.Db;
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

        public TechnologyControl()
        {
            InitializeComponent();

            this.chartDetail.ConstantLineMoved += ChartDetail_ConstantLineMoved;
            this.chartDetail.MouseUp += ChartDetail_MouseUp;
            this.chartDetail.CustomDrawCrosshair += ChartDetail_CustomDrawCrosshair;
        }

        bool _isRepaintCncLogColors = false;
        private void ChartDetail_CustomDrawCrosshair(object sender, CustomDrawCrosshairEventArgs e)
        {
            if (e.CrosshairElementGroups.Count == 0)
                return;

            var upModel = e.CrosshairElementGroups.FirstOrDefault()?.CrosshairElements.FirstOrDefault()?.SeriesPoint.Tag as UpModel;

            if (upModel == null)
                return;

            if (_selectedUp != null && _selectedUp == upModel)
                return;

            _selectedUp = upModel;
            _isRepaintCncLogColors = true;

            tmAutoZoom.Interval = 50;
        }

        private void ChartDetail_MouseUp(object sender, MouseEventArgs e)
        {
            if (this._analisis == null)
                return;

            if (_isChartDetailConstantLineMoved)
            {
                _isChartDetailConstantLineMoved = false;
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
                this.AddSeriesDetail(this._analisis);
            }
        }

        bool _isChartDetailConstantLineMoved = false;
        bool _isTimeConstantLineMoved = false;

        private void ChartDetail_ConstantLineMoved(object sender, ConstantLineMovedEventArgs e)
        {
            _isChartDetailConstantLineMoved = true;

            if (e.ConstantLine == UCLTimeLine || e.ConstantLine == LCLTimeLine)
            {
                _isTimeConstantLineMoved = true;
            }
        }

        private void sbLoad_Click(object sender, EventArgs e)
        {
            LoadData();

            var item = leMachineParam.GetSelectedDataRow() as RowX;

            if (item == null)
                return;

            AddSeries(item.List);
        }

        private void TechnologyControl_Load(object sender, EventArgs e)
        {
            this.PrepareUIElements();

            this.dtFromDate.DateTime = new DateTime(2020, 02, 01);
            this.dtToDate.DateTime = new DateTime(2020, 03, 1);

            this.AnalizeTool = new AnalizeTool(new TestIzm.Properties.Settings().UrlFiles);

            this.leMachine.Properties.DataSource = DataManager.Machines.ToArray();
            this.leMachine.ItemIndex = 1;
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

        private void leMachine_EditValueChanged(object sender, EventArgs e)
        {
            var machine = leMachine.GetSelectedDataRow() as MachineModel;
            var logList = DataManager.CncLog.Where(log => log.Machine.ID == machine.Id);
            this.gcUP.DataSource = logList;

            LoadData();
        }

        private void leMachineParam_EditValueChanged(object sender, EventArgs e)
        {
            var item = leMachineParam.GetSelectedDataRow() as RowX;

            if (item == null)
                return;

            this.AnalizeTool.SetMainParam(item.Id);

            AddSeries(item.List);
        }

        private void LoadData()
        {
            var machine = leMachine.GetSelectedDataRow() as MachineModel;

            if (machine == null)
                return;

            var dtFrom = dtFromDate.DateTime.Date.Add(new TimeSpan(teTimeFrom.Time.TimeOfDay.Ticks));
            var dtTo = dtToDate.DateTime.Date.Add(new TimeSpan(teTimeTo.Time.TimeOfDay.Ticks));

            AnalizeTool.Load(dtFrom, dtTo, machine);

            var propList = new List<RowX>();

            foreach (var p in machine.ParameterList)
            {
                p.List = AnalizeTool.GetTelemetry(p.Id);

                propList.Add(new RowX { Id = p.Id, Name = p.Name + (p.List == null ? "" : " - " + p.List.Count), List = p.List });
            }

            var index = 0; // propList.Count > 6 ? 6 : (propList.Count - 1);

            leMachineParam.Properties.DataSource = propList;
            leMachineParam.ItemIndex = index;

            leNextMachineParam.Properties.Items.Clear();
            leNextMachineParam.Properties.Items.AddRange(propList);
            // leNextMachineParam.SelectedIndex = index - 1;

            AddSeries(propList[index].List);

            BuildChartPie();
        }

        UpSeriesAnalisis _analisis;
        void AddSeriesDetail(UpSeriesAnalisis analisis)
        {
            _analisis = analisis;
            var chart = chartDetail;
            chart.BeginInit();
            chart.Series.Clear();
            var rowX = this.GetItemParam();

            var seriesAvg = new Series("Avg", ViewType.Bar);
            var seriesTime = new Series("Time", ViewType.Line);

            var valueTypeId = cbAvgValues.SelectedIndex;

            if (analisis == null)
                return;

            var list = analisis.UpModelList.ToList();

            if (this.ceIsHideOthers.Checked)
                list = list.Where(m => m.UpType != UpModel.UpTypeEnum.Other).ToList();

            seriesAvg.Points.AddRange(list.Select(a => AddSeriesPointChart(a, a.Index, GetValue(a, rowX.Id, valueTypeId))).ToArray());
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

            if (cbVisuals.Properties.Items[1].CheckState != CheckState.Checked)
            {
                this.Meenline.Visible = false;
                this.UCLline.Visible = false;
                this.LCLline.Visible = false;
                this.UCLWarningline.Visible = false;
                this.LCLWarningline.Visible = false;
            }

            if (cbVisuals.Properties.Items[0].CheckState != CheckState.Checked)
            {
                this.MeenTimeLine.Visible = false;
                this.UCLTimeLine.Visible = false;
                this.LCLTimeLine.Visible = false;
            }
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

        List<AdditionalSignalRow> _listData;
        void AddSeries(List<AdditionalSignalRow> list)
        {
            _listData = list;
            chartControl1.BeginInit();
            chartControl1.Series.Clear();

            var series = new Series();

            var view = new SwiftPlotSeriesView();
            series.View = view;

            series.Points.AddRange(list.Select(a => new SeriesPoint(new DateTime(2000, 1, 1).AddSeconds(a.DatetimeSeconds), a.Value)).ToArray());
            chartControl1.Series.Add(series);

            chartControl1.EndInit();
            var diagram = (chartControl1.Diagram as SwiftPlotDiagram);
            diagram.AxisX.ConstantLines.Clear();

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

            if (leNextMachineParam.SelectedIndex >= 0)
            {
                var item = leNextMachineParam.SelectedItem as RowX;

                var pane = new XYDiagramPane("Next");

                diagram.Panes.Add(pane);

                series = new Series();
                var view2 = new SwiftPlotSeriesView();
                series.View = view2;
                series.Points.AddRange(item.List.Select(a => new SeriesPoint(new DateTime(2000, 1, 1).AddSeconds(a.DatetimeSeconds), a.Value)).ToArray());
                view2.Pane = pane;

                chartControl1.CrosshairOptions.ShowOnlyInFocusedPane = false;
                chartControl1.Series.Add(series);

                diagram.SecondaryAxesY.Add(new SwiftPlotDiagramSecondaryAxisY("My Axis Y"));
                diagram.SecondaryAxesY[0].Alignment = AxisAlignment.Near;

                view2.AxisY = diagram.SecondaryAxesY[0];
            }

            diagram.EnableAxisXZooming = true;
            diagram.EnableAxisXScrolling = true;
            diagram.AxisY.VisualRange.Auto = true;

            diagram.AxisY.Strips.Clear();

            _isNeedZoom = true;
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

        // OperatingProgramExecutionLog up;
        DateTime lastMouseDatetime;
        private void chartControl1_CustomDrawCrosshair(object sender, CustomDrawCrosshairEventArgs e)
        {
            if (e.CrosshairElementGroups.Count == 0)
                return;

            var itemParam = GetItemParam();

            var group = e.CrosshairElementGroups.First();
            lastMouseDatetime = group.CrosshairElements[0].SeriesPoint.DateTimeArgument;

            var upModel = this.AnalizeTool.GetUpModel(lastMouseDatetime);

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

                var mark = upModel.GetMark(itemParam.Id);

                if (mark != null)
                {
                    upInfo += itemParam.Name + Environment.NewLine + " Avg: " + mark.Avg.ToString("0.00") + Environment.NewLine;
                    upInfo += " Avg diff: " + mark.AvgDiff.ToString("0.00") + Environment.NewLine;
                    upInfo += " Dispersion: " + mark.Disp.ToString("0.00") + Environment.NewLine;
                }
                else
                {
                    upInfo += " no mark: " + itemParam.Name;
                }

                if (upModel.Up.DtEnd > lastMouseDatetime)
                    state = "running";
            }

            if (group.CrosshairElements[0] != null)
                group.HeaderElement.Text = String.Format("Cnc: {1} State: {2} {3}{4}Datetime: {0:yyyy.MM.dd HH:mm:ss}", lastMouseDatetime, upName, state, Environment.NewLine, upInfo);

        }

        private RowX GetItemParam()
        {
            var itemParam = leMachineParam.GetSelectedDataRow() as RowX;
            var listParams = leMachineParam.Properties.DataSource as List<RowX>;
            if (itemParam == null)
                itemParam = listParams.FirstOrDefault();
            return itemParam;
        }

        bool isShowAll = true;
        private void sbHideAll_Click(object sender, EventArgs e)
        {
            isShowAll = !isShowAll;

            lcgLevels.Visibility = isShowAll ? DevExpress.XtraLayout.Utils.LayoutVisibility.Always : DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
            lcgOtcl.Visibility = isShowAll ? DevExpress.XtraLayout.Utils.LayoutVisibility.Always : DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
        }

        private void chartControl1_Zoom(object sender, ChartZoomEventArgs e)
        {
            _isNeedZoom = true; ;
        }

        private void AutoZoomY()
        {
            var diagram = (chartControl1.Diagram as SwiftPlotDiagram);

            if (diagram == null)
                return;

            var lst = _listData.Where(a => a.Datetime >= (DateTime)diagram.AxisX.VisualRange.MinValue && a.Datetime <= (DateTime)diagram.AxisX.VisualRange.MaxValue);
            var minY = lst.Min(a => a.Value) * 0.9;
            var maxY = lst.Max(a => a.Value) * 1.11;

            diagram.AxisY.VisualRange.MinValue = minY;
            diagram.AxisY.VisualRange.MaxValue = maxY;

            if (leNextMachineParam.SelectedItem != null)
            {
                var rowx = leNextMachineParam.SelectedItem as RowX;

                lst = rowx.List.Where(a => a.Datetime >= (DateTime)diagram.AxisX.VisualRange.MinValue && a.Datetime <= (DateTime)diagram.AxisX.VisualRange.MaxValue);

                minY = lst.Min(a => a.Value) * 0.9;
                maxY = lst.Max(a => a.Value) * 1.11;

                var axis = (chartControl1.Series[1].View as SwiftPlotSeriesView).AxisY;

                axis.VisualRange.MinValue = minY;
                axis.VisualRange.MaxValue = maxY;
            }
        }

        private void chartControl1_Click(object sender, EventArgs e)
        {
            // diagram.AxisY.ConstantLines.Clear();
            // var diagram = (chartControl1.Diagram as SwiftPlotDiagram);
            // 
            // foreach (var seriesAnalisis in this.AnalizeTool.UpSeriesAnalisisList)
            // {
            //     //diagram.AxisX.ConstantLines.Add(new ConstantLine(seriesAnalisis.UpName, seriesAnalisis.DtFrom) { ShowInLegend = false });
            // }
            // 
            // var line = new ConstantLine(this.lastMouseDatetime.ToString(), this.lastMouseDatetime);
            // 
            // diagram.AxisX.ConstantLines.Add(line);
        }

        private void chartControl1_Scroll(object sender, ChartScrollEventArgs e)
        {
            _isNeedZoom = true;
        }

        private void chartControl1_Resize(object sender, EventArgs e)
        {
            _isNeedZoom = true;
        }

        private void chartControl1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var diagram = (chartControl1.Diagram as SwiftPlotDiagram);
                diagram.AxisX.Strips.Clear();
                _selectedUp = null;
            }
        }

        private void chartControl1_DoubleClick(object sender, EventArgs e)
        {
            _analisis = null;
            RepaintCncLogColors();
        }

        private void RepaintCncLogColors()
        {
            var analisis = _analisis;

            if (analisis == null)
                analisis = this.AnalizeTool.GetAnalisis(lastMouseDatetime);

            if (analisis == null)
                return;

            var itemParam = GetItemParam();

            var diagram = (chartControl1.Diagram as SwiftPlotDiagram);
            diagram.AxisX.Strips.Clear();

            analisis.ColorizeMost(itemParam.Id);

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

            AddSeriesDetail(analisis);
        }

        private void comboBoxEdit1_SelectedIndexChanged(object sender, EventArgs e)
        {
            AddSeriesDetail(_analisis);

        }

        private void leNextMachineParam_EditValueChanged(object sender, EventArgs e)
        {
            var item = leMachineParam.GetSelectedDataRow() as RowX;

            if (item == null)
                return;

            AddSeries(item.List);

        }

        private void ceIsHideOthers_CheckedChanged(object sender, EventArgs e)
        {
            AddSeriesDetail(_analisis);

        }

        private void chartDetail_QueryCursor(object sender, QueryCursorEventArgs e)
        {

        }

        private void chartDetail_SelectedItemsChanged(object sender, SelectedItemsChangedEventArgs e)
        {
            this.Text = e.NewItems.Count.ToString();
            var diagram = chartControl1.Diagram as XYDiagram;

            foreach (SeriesPoint item in e.NewItems)
            {
                var up = item.Tag as UpModel;

                var constantLine1 = new ConstantLine("CNC >" + up.Index);
                diagram.AxisY.ConstantLines.Add(constantLine1);
                constantLine1.AxisValue = up.Up.DtStart;

                constantLine1.ShowInLegend = false;

                constantLine1.Color = Color.DarkGray;
                constantLine1.LineStyle.DashStyle = DashStyle.Dash;
                constantLine1.LineStyle.Thickness = 2;
            }
        }

        private void chartDetail_SelectedItemsChanging(object sender, SelectedItemsChangingEventArgs e)
        {
            this.Text = e.NewItems.Count.ToString();
            var diagram = chartControl1.Diagram as XYDiagram;

        }

        private void leNextMachineParam_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind == DevExpress.XtraEditors.Controls.ButtonPredefines.Delete)
                this.leNextMachineParam.SelectedIndex = -1;
        }

        private void leNextMachineParam_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        bool _isNeedZoom = false;
        private void tmAutoZoom_Tick(object sender, EventArgs e)
        {
            if (_isNeedZoom)
                AutoZoomY();

            _isNeedZoom = false;

            if (_isRepaintCncLogColors)
            {
                _isRepaintCncLogColors = false;
                RepaintCncLogColors();
            }

            tmAutoZoom.Interval = 500;
        }

        private void chartDetail_Move(object sender, EventArgs e)
        {

        }

        UpModel _selectedUp = null;
        private void chartDetail_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void chartDetail_MouseLeave(object sender, EventArgs e)
        {
            _selectedUp = null;
            _isRepaintCncLogColors = true;
        }


        private void cbVisuals_EditValueChanged(object sender, EventArgs e)
        {
            AddSeriesDetail(_analisis);
        }

        private void cbAlternativeParam_SelectedIndexChanged(object sender, EventArgs e)
        {
            AddSeriesComparer(_analisis);
        }

        void AddSeriesComparer(UpSeriesAnalisis analisis)
        {
            _analisis = analisis;
            var chart = chartDetail;
            chart.BeginInit();
            chart.Series.Clear();
            var rowX = this.GetItemParam();

            // var seriesAvg = new Series("Avg", ViewType.Bar);
            var seriesTime = new Series("Time", ViewType.Bar);

            // Вообще тут хорошо бы иметь кластеризацию
            // разделим Time на 10 частей
            // var min = 

            var valueTypeId = cbAvgValues.SelectedIndex;

            if (analisis == null)
                return;

            var list = analisis.UpModelList.ToList();

            if (this.ceIsHideOthers.Checked)
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
    }
}
