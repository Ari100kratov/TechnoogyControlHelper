namespace WindowsFormsApp1
{
    public class Mark
    {
        public Mark(int paramId)
        {
            this.ParamId = paramId;
        }

        public int ParamId { get; set; }
        public double Avg { get; set; }
        public double Count { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double AvgDiff { get; set; }
        public double Disp { get; set; }
    }
}
