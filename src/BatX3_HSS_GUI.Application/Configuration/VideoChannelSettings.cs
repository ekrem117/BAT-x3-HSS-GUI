namespace BatX3_HSS_GUI.Application.Configuration
{
    public sealed class VideoChannelSettings
    {
        public int ListenPort { get; set; } = 2024;

        public int ReceiveBufferBytes { get; set; } = 4 * 1024 * 1024;
    }
}