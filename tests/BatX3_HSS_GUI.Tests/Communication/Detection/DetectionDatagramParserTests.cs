using BatX3_HSS_GUI.Domain.Detection;
using BatX3_HSS_GUI.Infrastructure.Communication.Detection;
using System.Text;

namespace BatX3_HSS_GUI.Tests.Communication.Detection
{
    public sealed class DetectionDatagramParserTests
    {
        [Fact]
        public void TryParse_ValidDetectionFrame_ShouldParse()
        {
            const string json =
                """
            {
              "frame_id": 42,
              "ts": 1234.5,
              "detections": [
                {
                  "id": 7,
                  "x": 100.5,
                  "y": 200.0,
                  "w": 80.0,
                  "h": 40.0,
                  "cls": 0,
                  "team": 1,
                  "conf": 0.92,
                  "parent_id": null,
                  "vx": 3.5,
                  "vy": -1.0,
                  "age": 0.4,
                  "predicted": false
                }
              ],
              "lock": {
                "track_id": 7,
                "state": "LOCKED",
                "state_name": "LOCKED",
                "cls": 0,
                "team": 1,
                "x": 140.5,
                "y": 220.0,
                "vx": 3.5,
                "vy": -1.0,
                "lost_for": 0.0,
                "shots_fired": 2,
                "reacquires": 1,
                "duration": 4.2
              }
            }
            """;

            byte[] datagram = Encoding.UTF8.GetBytes(json);

            bool success = DetectionDatagramParser.TryParse(datagram, out DetectionFrame? frame, out string? error);

            Assert.True(success);
            Assert.Null(error);
            Assert.NotNull(frame);

            Assert.Equal((uint)42, frame.FrameId);

            Assert.Single(frame.Detections);

            DetectionTarget target = frame.Detections[0];

            Assert.Equal(7, target.Id);

            Assert.Equal(0, target.ClassId);

            Assert.Equal(1, target.TeamId);

            Assert.False(target.Predicted);

            Assert.NotNull(frame.Lock);

            Assert.Equal(7, frame.Lock.TrackId);

            Assert.Equal("LOCKED", frame.Lock.State);
        }

        [Fact]
        public void TryParse_EmptyDetections_ShouldBeValid()
        {
            const string json =
                """
            {
                "frame_id": 100,
                "ts": 1234.5,
                "detections": [],
                "lock": null
            }
            """;

            byte[] datagram = Encoding.UTF8.GetBytes(json);

            bool success = DetectionDatagramParser.TryParse(datagram, out DetectionFrame? frame, out string? error);

            Assert.True(success);
            Assert.Null(error);
            Assert.NotNull(frame);

            Assert.Empty(frame.Detections);

            Assert.Null(frame.Lock);
        }

        [Fact]
        public void TryParse_InvalidJson_ShouldFail()
        {
            byte[] datagram = Encoding.UTF8.GetBytes("{ not-json }");

            bool success = DetectionDatagramParser.TryParse(datagram, out DetectionFrame? frame, out string? error);

            Assert.False(success);
            Assert.Null(frame);
            Assert.NotNull(error);
        }

        [Fact]
        public void TryParse_PredictedTarget_ShouldPreserveFlag()
        {
            const string json =
                """
            {
              "frame_id": 10,
              "ts": 100.0,
              "detections": [
                {
                  "id": 3,
                  "x": 10,
                  "y": 20,
                  "w": 30,
                  "h": 40,
                  "cls": 3,
                  "team": 2,
                  "conf": 0.75,
                  "parent_id": null,
                  "vx": 1.0,
                  "vy": 2.0,
                  "age": 0.2,
                  "predicted": true
                }
              ]
            }
            """;

            bool success = DetectionDatagramParser.TryParse(Encoding.UTF8.GetBytes(json), out DetectionFrame? frame, out string? error);

            Assert.True(success);
            Assert.Null(error);
            Assert.NotNull(frame);

            Assert.True(frame.Detections[0].Predicted);
        }
    }
}