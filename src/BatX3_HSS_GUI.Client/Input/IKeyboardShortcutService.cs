using System.Windows.Input;

namespace BatX3_HSS_GUI.Client.Input
{
    public interface IKeyboardShortcutService
    {
        bool IsCaptureActive { get; }

        void Reload();

        void BeginCapture(Action<Key, ModifierKeys> captureCompleted, Action? captureCanceled = null);

        void CancelCapture();

        bool TryCaptureKeyDown(Key key, ModifierKeys modifiers, bool isRepeat);

        Task<bool> TryPressAsync(Key key, ModifierKeys modifiers, CancellationToken cancellationToken = default);

        Task<bool> TryReleaseAsync(Key key, CancellationToken cancellationToken = default);

        Task ReleaseAllAsync(CancellationToken cancellationToken = default);
    }
}