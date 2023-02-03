using DLPTool.DLP;
using FRMLib.Scope;

namespace DLPTool
{
    public partial class Form1 : Form
    {
        ScopeController scopeController;
        TracesManager tracesManager;
        DLP_IO8 dlp;

        public Form1()
        {
            InitializeComponent();
            dlp = new DLP_IO8();
            scopeController = new ScopeController();
            tracesManager = new TracesManager();

            tracesManager.ScopeController = scopeController;    
            scopeView1.DataSource = scopeController;
            markerView1.DataSource = scopeController;
            traceView1.DataSource = scopeController;

            
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            dlp.Open();
            var r = await dlp.Ping();
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Interval = 1000;
            t.Tick += T_Tick;
            t.Start();

            ScopeSettingsManager.ApplySettings(scopeController);
            scopeController.Settings.SetHorizontal(DateTime.Now, DateTime.Now + TimeSpan.FromMinutes(5));
        }

        private async void T_Tick(object? sender, EventArgs e)
        {
            tracesManager.Channel1.Points.Add(new STDLib.Math.PointD(DateTime.Now.Ticks, await GetVoltage(Channels.CH1)));
            tracesManager.Channel2.Points.Add(new STDLib.Math.PointD(DateTime.Now.Ticks, await GetVoltage(Channels.CH2)));
            scopeController.RedrawAll();
        }

        async Task<float> GetVoltage(Channels channel)
        {
            var raw = await dlp.GetAnalogAsync(channel);
            return raw * 5f / 1024f;
        }

    }
}