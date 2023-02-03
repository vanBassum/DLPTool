using FRMLib.Scope;

namespace DLPTool
{
    public class TracesManager
    {
        public Trace Channel1 { get; }
        public Trace Channel2 { get; }

        private ScopeController? scopeController;
        public ScopeController? ScopeController
        {
            get { return scopeController; }
            set
            {
                scopeController = value;
                if (scopeController != null)
                    AttachTraces();
            }
        }

        public TracesManager()
        {
            Channel1 = new Trace { Name = "CH 1", Offset = 0, Scale = 2, Layer = 9, Unit = "V", Pen = Palettes.DistinctivePallet[0], DrawStyle = Trace.DrawStyles.Lines, DrawOption = Trace.DrawOptions.ShowScale };
            Channel2 = new Trace { Name = "CH 2", Offset = 0, Scale = 2, Layer = 9, Unit = "V", Pen = Palettes.DistinctivePallet[3], DrawStyle = Trace.DrawStyles.Lines, DrawOption = Trace.DrawOptions.ShowScale };

        }

        void AttachTraces()
        {
            if (ScopeController == null)
                return;
            ScopeController.Traces.Add(Channel1);
            ScopeController.Traces.Add(Channel2);
        }
    }
}