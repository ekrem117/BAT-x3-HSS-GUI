namespace BatX3_HSS_GUI.Application.Configuration
{
    public sealed class DetectionChannelSettings
    {
        public int ListenPort { get; set; } = 2025;

        public int StaleTimeoutMs { get; set; } = 500;
    }
}