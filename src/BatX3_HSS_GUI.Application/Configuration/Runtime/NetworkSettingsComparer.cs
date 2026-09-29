namespace BatX3_HSS_GUI.Application.Configuration.Runtime
{
    public static class NetworkSettingsComparer
    {
        public static bool RequiresRebind(NetworkSettings current, NetworkSettings target)
        {
            ArgumentNullException.ThrowIfNull(current);

            ArgumentNullException.ThrowIfNull(target);

            if (!string.Equals(current.ServerIp, target.ServerIp, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.Equals(current.LocalBindIp, target.LocalBindIp, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (current.Command.LocalPort != target.Command.LocalPort)
            {
                return true;
            }

            if (current.Command.RemotePort != target.Command.RemotePort)
            {
                return true;
            }

            if (current.Command.ResponseTimeoutMs != target.Command.ResponseTimeoutMs)
            {
                return true;
            }

            if (current.Video.ListenPort != target.Video.ListenPort)
            {
                return true;
            }

            if (current.Video.ReceiveBufferBytes != target.Video.ReceiveBufferBytes)
            {
                return true;
            }

            if (current.Detection.ListenPort != target.Detection.ListenPort)
            {
                return true;
            }

            if (current.Detection.StaleTimeoutMs != target.Detection.StaleTimeoutMs)
            {
                return true;
            }

            return false;
        }
    }
}