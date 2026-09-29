using BatX3_HSS_GUI.Application.Diagnostics;

namespace BatX3_HSS_GUI.Tests.Diagnostics
{
    public sealed class ChannelHealthServiceTests
    {
        [Fact]
        public void NewChannel_ShouldBeStopped()
        {
            ChannelHealthService service = new();

            ChannelHealthSnapshot snapshot = service.GetSnapshot(DiagnosticChannel.Video);

            Assert.Equal(ChannelHealthState.Stopped, snapshot.State);
        }

        [Fact]
        public void StartedChannelWithoutData_ShouldBeWaiting()
        {
            ChannelHealthService service = new();

            service.ReportStarted(DiagnosticChannel.Video, TimeSpan.FromSeconds(1));

            ChannelHealthSnapshot snapshot = service.GetSnapshot(DiagnosticChannel.Video);

            Assert.Equal(ChannelHealthState.Waiting, snapshot.State);
        }

        [Fact]
        public void SuccessfulChannel_ShouldBeHealthy()
        {
            ChannelHealthService service = new();

            service.ReportStarted(DiagnosticChannel.Video, TimeSpan.FromSeconds(1));

            service.ReportSuccess(DiagnosticChannel.Video);

            ChannelHealthSnapshot snapshot = service.GetSnapshot(DiagnosticChannel.Video);

            Assert.Equal(ChannelHealthState.Healthy, snapshot.State);

            Assert.Equal(1, snapshot.SuccessCount);
        }

        [Fact]
        public void ErrorAfterSuccess_ShouldBeDegraded()
        {
            ChannelHealthService service = new();

            service.ReportStarted(DiagnosticChannel.Command, TimeSpan.FromSeconds(1));

            service.ReportSuccess(DiagnosticChannel.Command);

            service.ReportError(DiagnosticChannel.Command, "Test error");

            ChannelHealthSnapshot snapshot = service.GetSnapshot(DiagnosticChannel.Command);

            Assert.Equal(ChannelHealthState.Degraded, snapshot.State);

            Assert.Equal(1, snapshot.ErrorCount);

            Assert.Equal("Test error", snapshot.LastError);
        }

        [Fact]
        public void StoppedChannel_ShouldBeStopped()
        {
            ChannelHealthService service = new();

            service.ReportStarted(DiagnosticChannel.Detection, TimeSpan.FromSeconds(1));

            service.ReportSuccess(DiagnosticChannel.Detection);

            service.ReportStopped(DiagnosticChannel.Detection);

            ChannelHealthSnapshot snapshot = service.GetSnapshot(DiagnosticChannel.Detection);

            Assert.Equal(ChannelHealthState.Stopped, snapshot.State);
        }
    }
}