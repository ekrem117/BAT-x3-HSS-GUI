using BatX3_HSS_GUI.Infrastructure.Communication.Command;
using System.Text;
using System.Text.Json;

namespace BatX3_HSS_GUI.Tests.Communication.Command
{
    public sealed class CommandMessageSerializerTests
    {
        [Fact]
        public void SerializeGet_ShouldCreateExpectedProtocolMessage()
        {
            byte[] data = CommandMessageSerializer.SerializeGet(1001, new[] { "control.pid.kp", "weapon.ammo" });

            string json = Encoding.UTF8.GetString(data);

            using JsonDocument document = JsonDocument.Parse(json);

            JsonElement root = document.RootElement;

            Assert.Equal("GET", root.GetProperty("Method").GetString());

            Assert.Equal(1001, root.GetProperty("messageID").GetInt32());

            Assert.Equal("?", root.GetProperty("control.pid.kp").GetString());

            Assert.Equal("?", root.GetProperty("weapon.ammo").GetString());
        }

        [Fact]
        public void SerializeSet_ShouldPreserveValueTypes()
        {
            Dictionary<string, object?> parameters = new()
            {
                ["control.pid.kp"] = 3.0,
                ["weapon.armed"] = 1
            };

            byte[] data = CommandMessageSerializer.SerializeSet(1002, parameters);

            using JsonDocument document = JsonDocument.Parse(data);

            JsonElement root = document.RootElement;

            Assert.Equal("SET", root.GetProperty("Method").GetString());

            Assert.Equal(3.0, root.GetProperty("control.pid.kp").GetDouble());

            Assert.Equal(1, root.GetProperty("weapon.armed").GetInt32());
        }

        [Fact]
        public void SerializeSet_ShouldPreserveFloatingPointWireType()
        {
            IReadOnlyDictionary<string, object?> values =
                new Dictionary<string, object?>
                {
                    ["motion.analog.pan"] =
                        0.0,

                    ["motion.analog.tilt"] =
                        1.0,

                    ["motion.analog.precision"] =
                        0
                };

            byte[] payload =
                CommandMessageSerializer.SerializeSet(
                    42,
                    values);

            using JsonDocument document =
                JsonDocument.Parse(
                    payload);

            JsonElement root =
                document.RootElement;

            Assert.Equal(
                "0.0",
                root
                    .GetProperty(
                        "motion.analog.pan")
                    .GetRawText());

            Assert.Equal(
                "1.0",
                root
                    .GetProperty(
                        "motion.analog.tilt")
                    .GetRawText());

            Assert.Equal(
                "0",
                root
                    .GetProperty(
                        "motion.analog.precision")
                    .GetRawText());
        }

        [Theory]
        [InlineData(-1.0, "-1.0")]
        [InlineData(0.0, "0.0")]
        [InlineData(1.0, "1.0")]
        [InlineData(0.25, "0.25")]
        public void SerializeSet_ShouldKeepDoubleAsFloatToken(
    double value,
    string expectedRawText)
        {
            IReadOnlyDictionary<string, object?> values =
                new Dictionary<string, object?>
                {
                    ["motion.analog.pan"] =
                        value
                };

            byte[] payload =
                CommandMessageSerializer.SerializeSet(
                    1,
                    values);

            using JsonDocument document =
                JsonDocument.Parse(
                    payload);

            Assert.Equal(
                expectedRawText,
                document.RootElement
                    .GetProperty(
                        "motion.analog.pan")
                    .GetRawText());
        }
    }
}