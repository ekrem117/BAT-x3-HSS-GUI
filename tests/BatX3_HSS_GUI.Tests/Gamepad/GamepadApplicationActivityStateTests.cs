using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Tests.Gamepad
{
    public sealed class
            GamepadApplicationActivityStateTests
    {
        [Fact]
        public void InitialState_ShouldBeActive()
        {
            GamepadApplicationActivityState state =
                new();

            GamepadApplicationActivitySnapshot snapshot =
                state.GetSnapshot();

            Assert.True(
                snapshot.IsActive);

            Assert.Equal(
                0,
                snapshot.Version);
        }

        [Fact]
        public void StateTransition_ShouldAdvanceVersion()
        {
            GamepadApplicationActivityState state =
                new();

            state.SetActive(
                false);

            GamepadApplicationActivitySnapshot inactive =
                state.GetSnapshot();

            Assert.False(
                inactive.IsActive);

            Assert.Equal(
                1,
                inactive.Version);

            state.SetActive(
                true);

            GamepadApplicationActivitySnapshot active =
                state.GetSnapshot();

            Assert.True(
                active.IsActive);

            Assert.Equal(
                2,
                active.Version);
        }

        [Fact]
        public void EquivalentState_ShouldNotAdvanceVersion()
        {
            GamepadApplicationActivityState state =
                new();

            state.SetActive(
                true);

            Assert.Equal(
                0,
                state.GetSnapshot()
                    .Version);
        }
    }
}