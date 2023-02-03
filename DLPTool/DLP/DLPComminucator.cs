using System.IO.Ports;

namespace DLPTool.DLP
{
    public class DLPComminucator
    {
        SerialPort port = new();
        public void Open()
        {
            port.PortName = "COM17";
            port.BaudRate = 115200;
            port.Open();
            Send(0x5C);
        }

        public void Close()
        {
            port.Close();
        }

        protected async Task<byte[]> Execute(Cmd command, CancellationToken cancellationToken = default)
        {
            if (command.RxLength == 0)
            {
                Send(command.Command);
                return new byte[0];
            }
            else
            {
                return await TransmitAsync(command.Command, command.RxLength, cancellationToken);
            }
        }

        private async Task<byte[]> TransmitAsync(byte data, int rxLength, CancellationToken cancellationToken = default)
        {
            Send(data);
            int len = port.BytesToRead;
            while(len < rxLength)
            {
                await Task.Delay(1, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                len = port.BytesToRead;
            }
            byte[] result = new byte[len];
            port.Read(result, 0, len);
            return result;
        }

        protected void Send(byte data)
        {
            port.Write(new byte[] { data }, 0, 1);
        }
    }




}