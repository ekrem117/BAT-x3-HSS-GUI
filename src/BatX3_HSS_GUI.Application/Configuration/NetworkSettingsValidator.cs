using System.Net;
using System.Net.Sockets;

namespace BatX3_HSS_GUI.Application.Configuration
{
    public static class NetworkSettingsValidator
    {
        public static IReadOnlyList<string> Validate(NetworkSettings settings)
        {
            List<string> errors = new();

            ValidateIpv4(settings.ServerIp, "Server IP", errors);

            ValidateIpv4(settings.LocalBindIp, "Local Bind IP", errors);

            ValidatePort(settings.Command.RemotePort, "Command Remote Port", allowZero: false, errors);

            ValidatePort(settings.Command.LocalPort, "Command Local Port", allowZero: true, errors);

            ValidatePort(settings.Video.ListenPort, "Video Listen Port", allowZero: false, errors);

            ValidatePort(settings.Detection.ListenPort, "Detection Listen Port", allowZero: false, errors);

            if (settings.Command.ResponseTimeoutMs <= 0)
            {
                errors.Add("Command response timeout sıfırdan büyük olmalıdır.");
            }

            if (settings.Video.ReceiveBufferBytes < 4 * 1024 * 1024)
            {
                errors.Add("Video receive buffer en az 4 MB olmalıdır.");
            }

            if (settings.Detection.StaleTimeoutMs <= 0)
            {
                errors.Add("Detection stale timeout sıfırdan büyük olmalıdır.");
            }

            if (settings.Command.RemotePort == settings.Video.ListenPort ||
                settings.Command.RemotePort == settings.Detection.ListenPort ||
                settings.Video.ListenPort == settings.Detection.ListenPort)
            {
                errors.Add("Command, Video ve Detection portları birbirinden farklı olmalıdır.");
            }

            return errors;
        }

        private static void ValidateIpv4(string value, string displayName, ICollection<string> errors)
        {
            if (!IPAddress.TryParse(value, out IPAddress? address))
            {
                errors.Add($"{displayName} geçerli bir IP adresi değildir.");
                return;
            }

            if (address.AddressFamily != AddressFamily.InterNetwork)
            {
                errors.Add($"{displayName} geçerli bir IPv4 adresi olmalıdır.");
            }
        }

        private static void ValidatePort(int port, string displayName, bool allowZero, ICollection<string> errors)
        {
            if (allowZero && port == 0)
            {
                return;
            }

            if (port is < 1 or > 65535)
            {
                errors.Add($"{displayName} 1 ile 65535 arasında olmalıdır.");
            }
        }
    }
}