using BatX3_HSS_GUI.Domain.Communication.Command;
using BatX3_HSS_GUI.Infrastructure.Communication.Command;
using System.Text;

namespace BatX3_HSS_GUI.Tests.Communication.Command
{
    public sealed class CommandMessageParserTests
    {
        [Fact]
        public void TryParse_ShouldParseGetResponse()
        {
            const string json =
                """
                {
                  "Method": "GETRSP",
                  "messageID": 1001,
                  "control.pid.kp": {
                    "status": 0,
                    "value": 2.5
                  },
                  "yok.olan": {
                    "status": 2,
                    "value": null
                  }
                }
                """;

            bool parsed = CommandMessageParser.TryParse(Encoding.UTF8.GetBytes(json), out CommandResponse? response, out string? error);

            Assert.True(parsed, error);
            Assert.NotNull(response);

            Assert.Equal(CommandResponseMethod.GetResponse, response.Method);

            Assert.Equal(1001, response.MessageId);

            CommandParameterResult kp = response.Parameters["control.pid.kp"];

            Assert.Equal(CommandStatus.Success, kp.Status);

            Assert.Equal(2.5, Assert.IsType<double>(kp.Value));

            CommandParameterResult missing = response.Parameters["yok.olan"];

            Assert.Equal(CommandStatus.UndefinedKey, missing.Status);

            Assert.Null(missing.Value);
        }

        [Fact]
        public void TryParse_ShouldParseErrorResponse()
        {
            const string json =
                """
                {
                  "Method": "ERRRSP",
                  "messageID": 1004,
                  "status": 1
                }
                """;

            bool parsed = CommandMessageParser.TryParse(Encoding.UTF8.GetBytes(json), out CommandResponse? response, out string? error);

            Assert.True(parsed, error);
            Assert.NotNull(response);

            Assert.True(response.IsError);

            Assert.Equal(CommandStatus.UndefinedMethodType, response.ErrorStatus);
        }
    }
}