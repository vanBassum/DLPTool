using FRMLib.Scope;

namespace DLPTool
{
    public class ScopeSettingsManager
    {
        public static void ApplySettings(ScopeController scopeController)
        {
            scopeController.Settings.BackgroundColor = System.Drawing.Color.Black;
            scopeController.Settings.DrawScalePosVertical = DrawPosVertical.Right;
            scopeController.Settings.DrawScalePosHorizontal = DrawPosHorizontal.Bottom;
            scopeController.Settings.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            scopeController.Settings.GridZeroPosition = FRMLib.Scope.VerticalZeroPosition.Middle;
            scopeController.Settings.HorizontalDivisions = 10;
            scopeController.Settings.HorOffset = 0D;
            scopeController.Settings.HorScale = 10D;
            scopeController.Settings.VerticalDivisions = 8;
            scopeController.Settings.ZeroPosition = FRMLib.Scope.VerticalZeroPosition.Middle;
            scopeController.Settings.HorizontalToHumanReadable = (ticks) => {
                if (double.IsNaN(ticks)) return "NaN";
                if (double.IsInfinity(ticks)) return "Inf";
                if (ticks > DateTime.MaxValue.Ticks) return "Inf";
                if (ticks < DateTime.MinValue.Ticks) return "-Inf";

                string result = "";
                try
                {
                    DateTime dt = new DateTime((long)ticks);
                    result = dt.ToString("dd-MM-yyyy") + " \r\n" + dt.ToString("HH:mm:ss");
                }
                catch { }
                return result;
            };


        }
    }
}