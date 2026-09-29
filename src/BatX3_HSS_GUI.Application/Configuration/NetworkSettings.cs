namespace BatX3_HSS_GUI.Application.Configuration
{
    public sealed class NetworkSettings
    {
        public const string SectionName = "Network";

        public string ServerIp { get; set; } = "127.0.0.1";

        public string LocalBindIp { get; set; } = "127.0.0.1";

        public CommandChannelSettings Command { get; set; } = new();

        public VideoChannelSettings Video { get; set; } = new();

        public DetectionChannelSettings Detection { get; set; } = new();
    }
}