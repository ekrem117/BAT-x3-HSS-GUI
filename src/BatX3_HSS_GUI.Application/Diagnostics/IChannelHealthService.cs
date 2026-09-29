namespace BatX3_HSS_GUI.Application.Diagnostics
{
    public interface IChannelHealthService
    {
        void ReportStarted(DiagnosticChannel channel, TimeSpan staleAfter);

        void ReportSuccess(DiagnosticChannel channel);

        void ReportError(DiagnosticChannel channel, string error);

        void ReportStopped(DiagnosticChannel channel);

        ChannelHealthSnapshot GetSnapshot(DiagnosticChannel channel);

        IReadOnlyList<ChannelHealthSnapshot> GetAllSnapshots();
    }
}