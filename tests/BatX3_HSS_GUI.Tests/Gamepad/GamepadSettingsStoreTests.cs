using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class GamepadSettingsStoreTests
    {
        [Fact]
        public void InitialSnapshot_ShouldUseClonedSettings()
        {
            GamepadSettings source =
                new()
                {
                    Deadzone =
                        0.15
                };

            GamepadSettingsStore store =
                new(
                    source);

            source.Deadzone =
                0.90;

            GamepadSettingsSnapshot snapshot =
                store.GetSnapshot();

            Assert.Equal(
                0.15,
                snapshot.Settings.Deadzone);
        }

        [Fact]
        public void EquivalentReplace_ShouldNotIncrementVersion()
        {
            GamepadSettingsStore store =
                new(
                    new GamepadSettings());

            long initialVersion =
                store.GetSnapshot().Version;

            store.Replace(
                new GamepadSettings());

            Assert.Equal(
                initialVersion,
                store.GetSnapshot().Version);
        }

        [Fact]
        public void ChangedReplace_ShouldIncrementVersion()
        {
            GamepadSettingsStore store =
                new(
                    new GamepadSettings
                    {
                        Deadzone =
                            0.15
                    });

            long initialVersion =
                store.GetSnapshot().Version;

            store.Replace(
                new GamepadSettings
                {
                    Deadzone =
                        0.30
                });

            Assert.True(
                store.GetSnapshot().Version >
                initialVersion);
        }

        [Fact]
        public void Replace_ShouldAtomicallyExposeNewSnapshot()
        {
            GamepadSettingsStore store =
                new(
                    new GamepadSettings
                    {
                        Deadzone =
                            0.15,

                        PollingIntervalMs =
                            10
                    });

            GamepadSettings replacement =
                new()
                {
                    Deadzone =
                        0.30,

                    PollingIntervalMs =
                        25
                };

            store.Replace(
                replacement);

            GamepadSettingsSnapshot snapshot =
                store.GetSnapshot();

            Assert.Equal(
                0.30,
                snapshot.Settings.Deadzone);

            Assert.Equal(
                25,
                snapshot.Settings.PollingIntervalMs);
        }

        [Fact]
        public void Replace_ShouldDeepCloneBindings()
        {
            GamepadSettings replacement =
                new();

            replacement.Deadzone =
                0.30;

            GamepadSettingsStore store =
                new(
                    new GamepadSettings());

            store.Replace(
                replacement);

            replacement.Bindings[0].Control =
                GamepadControl.X;

            GamepadSettingsSnapshot snapshot =
                store.GetSnapshot();

            Assert.NotEqual(
                GamepadControl.X,
                snapshot.Settings.Bindings[0].Control);
        }

        [Fact]
        public void PreviousSnapshot_ShouldRemainStableAfterReplace()
        {
            GamepadSettingsStore store =
                new(
                    new GamepadSettings
                    {
                        Deadzone =
                            0.15
                    });

            GamepadSettingsSnapshot previous =
                store.GetSnapshot();

            store.Replace(
                new GamepadSettings
                {
                    Deadzone =
                        0.40
                });

            Assert.Equal(
                0.15,
                previous.Settings.Deadzone);

            Assert.Equal(
                0.40,
                store.GetSnapshot()
                    .Settings
                    .Deadzone);
        }

        [Fact]
        public void InvalidReplacement_ShouldLeaveCurrentSnapshotUntouched()
        {
            GamepadSettingsStore store =
                new(
                    new GamepadSettings
                    {
                        Deadzone =
                            0.15
                    });

            GamepadSettingsSnapshot previous =
                store.GetSnapshot();

            GamepadSettings invalid =
                new()
                {
                    Deadzone =
                        1.50
                };

            Assert.Throws<
                InvalidOperationException>(
                    () =>
                        store.Replace(
                            invalid));

            GamepadSettingsSnapshot current =
                store.GetSnapshot();

            Assert.Equal(
                previous.Version,
                current.Version);

            Assert.Equal(
                0.15,
                current.Settings.Deadzone);
        }
    }
}