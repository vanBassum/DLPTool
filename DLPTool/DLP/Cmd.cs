
namespace DLPTool.DLP
{
    public class Cmd
    {


        public byte Command { get; }
        public int RxLength { get; set; }

        public Cmd(byte command, int rxLength)
        {
            Command = command;
            RxLength = rxLength;
        }

        public static Cmd SetHigh(Channels channel) => new Cmd(GetCommand(channel, Commands.HIGH), 0);
        public static Cmd SetLow(Channels channel) => new Cmd(GetCommand(channel, Commands.LOW), 0);
        public static Cmd GetDig(Channels channel) => new Cmd(GetCommand(channel, Commands.DIN), 1);
        public static Cmd GetAna(Channels channel) => new Cmd(GetCommand(channel, Commands.AIN), 2);
        public static Cmd Ping() => new Cmd(0x27, 1);


        public enum Commands
        {
            HIGH,
            LOW,
            DIN,
            AIN,
            TEMP,
        }

        static byte GetCommand(Channels channel, Commands cmd)
        {
            switch (channel, cmd)
            {
                case (Channels.CH1, Commands.HIGH): return 0x31;
                case (Channels.CH1, Commands.LOW): return 0x51;
                case (Channels.CH1, Commands.DIN): return 0x41;
                case (Channels.CH1, Commands.AIN): return 0x5A;
                case (Channels.CH1, Commands.TEMP): return 0x39;
                case (Channels.CH2, Commands.HIGH): return 0x32;
                case (Channels.CH2, Commands.LOW): return 0x57;
                case (Channels.CH2, Commands.DIN): return 0x53;
                case (Channels.CH2, Commands.AIN): return 0x58;
                case (Channels.CH2, Commands.TEMP): return 0x30;
                case (Channels.CH3, Commands.HIGH): return 0x33;
                case (Channels.CH3, Commands.LOW): return 0x45;
                case (Channels.CH3, Commands.DIN): return 0x44;
                case (Channels.CH3, Commands.AIN): return 0x43;
                case (Channels.CH3, Commands.TEMP): return 0x2D;
                case (Channels.CH4, Commands.HIGH): return 0x34;
                case (Channels.CH4, Commands.LOW): return 0x52;
                case (Channels.CH4, Commands.DIN): return 0x46;
                case (Channels.CH4, Commands.AIN): return 0x56;
                case (Channels.CH4, Commands.TEMP): return 0x3D;
                case (Channels.CH5, Commands.HIGH): return 0x35;
                case (Channels.CH5, Commands.LOW): return 0x54;
                case (Channels.CH5, Commands.DIN): return 0x47;
                case (Channels.CH5, Commands.AIN): return 0x42;
                case (Channels.CH5, Commands.TEMP): return 0x4F;
                case (Channels.CH6, Commands.HIGH): return 0x36;
                case (Channels.CH6, Commands.LOW): return 0x59;
                case (Channels.CH6, Commands.DIN): return 0x48;
                case (Channels.CH6, Commands.AIN): return 0x4E;
                case (Channels.CH6, Commands.TEMP): return 0x50;
                case (Channels.CH7, Commands.HIGH): return 0x37;
                case (Channels.CH7, Commands.LOW): return 0x55;
                case (Channels.CH7, Commands.DIN): return 0x4A;
                case (Channels.CH7, Commands.AIN): return 0x4D;
                case (Channels.CH7, Commands.TEMP): return 0x5B;
                case (Channels.CH8, Commands.HIGH): return 0x38;
                case (Channels.CH8, Commands.LOW): return 0x49;
                case (Channels.CH8, Commands.DIN): return 0x4B;
                case (Channels.CH8, Commands.AIN): return 0x2C;
                case (Channels.CH8, Commands.TEMP): return 0x5D;
                default: return 0;
            }
        }
    }




}