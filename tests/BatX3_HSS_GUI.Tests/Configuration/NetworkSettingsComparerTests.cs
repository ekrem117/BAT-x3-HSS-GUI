using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Configuration.Runtime;

namespace BatX3_HSS_GUI.Tests.Configuration
{
    public sealed class NetworkSettingsComparerTests
    {
        [Fact]
        public void IdenticalSettings_ShouldNotRequireRebind()
        {
            NetworkSettings current = CreateSettings();

            NetworkSettings target = CreateSettings();

            bool result = NetworkSettingsComparer.RequiresRebind(current, target);

            Assert.False(result);
        }

        [Fact]
        public void VideoPortChange_ShouldRequireRebind()
        {
            NetworkSettings current = CreateSettings();

            NetworkSettings target = CreateSettings();

            target.Video.ListenPort = 2124;

            bool result = NetworkSettingsComparer.RequiresRebind(current, target);

            Assert.True(result);
        }

        [Fact]
        public void ServerIpChange_ShouldRequireRebind()
        {
            NetworkSettings current = CreateSettings();

            NetworkSettings target = CreateSettings();

            target.ServerIp = "127.0.0.2";

            bool result = NetworkSettingsComparer.RequiresRebind(current, target);

            Assert.True(result);
        }

        private static NetworkSettings CreateSettings()
        {
            return new NetworkSettings
            {
                ServerIp = "127.0.0.1",

                LocalBindIp = "127.0.0.1",

                Command =
            {
                LocalPort =
                    0,

                RemotePort =
                    2023,

                ResponseTimeoutMs =
                    300
            },

                Video =
            {
                ListenPort =
                    2024,

                ReceiveBufferBytes =
                    4 * 1024 * 1024
            },

                Detection =
            {
                ListenPort =
                    2025,

                StaleTimeoutMs =
                    500
            }
            };
        }
    }
}