namespace DLPTool.DLP
{
    public class DLP_IO8 : DLPComminucator
    {
        public async Task<bool> Ping(CancellationToken cancellationToken = default)
        {
            return (await Execute(Cmd.Ping())).FirstOrDefault() == 0x51;
        }

        public async Task Set(Channels channel, bool value, CancellationToken cancellationToken = default)
        {
            if (value)
                await Execute(Cmd.SetHigh(channel), cancellationToken);
            else
                await Execute(Cmd.SetLow(channel), cancellationToken);
        }

        public async Task<bool> GetDigitalAsync(Channels channel, CancellationToken cancellationToken = default)
        {
            var r = await Execute(Cmd.GetDig(channel), cancellationToken);
            return r?.FirstOrDefault() == 0x01;
        }

        public async Task<ushort> GetAnalogAsync(Channels channel, CancellationToken cancellationToken = default)
        {
            var r = await Execute(Cmd.GetAna(channel), cancellationToken);
            return (ushort)((r[0] << 8) + r[1]);
        }


    }




}