namespace BatX3_HSS_GUI.Application.Configuration
{
    public sealed class CommandChannelSettings
    {
        public int RemotePort { get; set; } = 2023;

        public int LocalPort { get; set; }

        public int ResponseTimeoutMs { get; set; } = 300;
    }
}