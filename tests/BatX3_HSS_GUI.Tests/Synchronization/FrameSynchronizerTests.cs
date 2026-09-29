using BatX3_HSS_GUI.Application.Synchronization;
using BatX3_HSS_GUI.Domain.Detection;
using BatX3_HSS_GUI.Domain.Synchronization;
using BatX3_HSS_GUI.Domain.Video;

namespace BatX3_HSS_GUI.Tests.Synchronization
{
    public sealed class FrameSynchronizerTests
    {
        [Fact]
        public void DetectionBeforeVideo_ExactFrameId_ShouldMatchOnNextVideo()
        {
            FrameSynchronizer synchronizer = new(bufferDepth: 1);

            List<SynchronizedFrame> results = new();

            synchronizer.FrameReady += (_, args) => { results.Add(args.Frame); };

            synchronizer.PushDetection(CreateDetectionFrame(42));

            synchronizer.PushVideo(CreateVideoFrame(42));

            Assert.Empty(results);

            synchronizer.PushVideo(CreateVideoFrame(43));

            Assert.Single(results);

            Assert.True(results[0].HasDetection);

            Assert.Equal((uint)42, results[0].Video.FrameId);

            Assert.Equal((uint)42, results[0].Detection!.FrameId);
        }

        [Fact]
        public void VideoBeforeDetection_ExactFrameId_ShouldMatchOnNextVideo()
        {
            FrameSynchronizer synchronizer = new(bufferDepth: 1);

            List<SynchronizedFrame> results = new();

            synchronizer.FrameReady += (_, args) => { results.Add(args.Frame); };

            synchronizer.PushVideo(CreateVideoFrame(100));

            synchronizer.PushDetection(CreateDetectionFrame(100));

            Assert.Empty(results);

            synchronizer.PushVideo(CreateVideoFrame(101));

            Assert.Single(results);

            Assert.True(results[0].HasDetection);

            Assert.Equal((uint)100, results[0].FrameId);

            Assert.Equal((uint)100, results[0].Detection!.FrameId);
        }       

        [Fact]
        public void DifferentFrameIds_ShouldNotMatch()
        {
            FrameSynchronizer synchronizer = new();

            SynchronizedFrame? result = null;

            synchronizer.FrameReady += (_, args) => { result = args.Frame; };

            synchronizer.PushVideo(CreateVideoFrame(10));

            synchronizer.PushDetection(CreateDetectionFrame(9));

            Assert.Null(result);
        }

        [Fact]
        public void BufferOverflow_ShouldPublishPreviousVideoWithoutDetection()
        {
            FrameSynchronizer synchronizer = new(bufferDepth: 1);

            List<SynchronizedFrame> results = new();

            synchronizer.FrameReady += (_, args) => { results.Add(args.Frame); };

            synchronizer.PushVideo(CreateVideoFrame(1));

            Assert.Empty(results);

            synchronizer.PushVideo(CreateVideoFrame(2));

            Assert.Single(results);

            Assert.Equal((uint)1, results[0].FrameId);

            Assert.False(results[0].HasDetection);
        }

        [Fact]
        public void OldDetection_ShouldNeverMatchNewVideo()
        {
            FrameSynchronizer synchronizer = new(bufferDepth: 3);

            List<SynchronizedFrame> results = new();

            synchronizer.FrameReady += (_, args) => { results.Add(args.Frame); };

            synchronizer.PushDetection(CreateDetectionFrame(1));

            synchronizer.PushDetection(CreateDetectionFrame(2));

            synchronizer.PushDetection(CreateDetectionFrame(3));

            synchronizer.PushDetection(CreateDetectionFrame(4));

            synchronizer.PushVideo(CreateVideoFrame(5));

            Assert.Empty(results);
        }

        private static VideoFrame CreateVideoFrame(uint frameId)
        {
            return new VideoFrame
            {
                FrameId = frameId,
                Timestamp = frameId,
                Width = 1280,
                Height = 720,
                JpegBytes =
                [
                    0xFF,
                    0xD8,
                    0xFF,
                    0xD9
                ]
            };
        }

        private static DetectionFrame CreateDetectionFrame(uint frameId)
        {
            return new DetectionFrame
            {
                FrameId = frameId,
                Timestamp = frameId,
                Detections = Array.Empty<DetectionTarget>(),
                Lock = null
            };
        }

        [Fact]
        public void MatchedFrame_ShouldAlwaysHaveSameVideoAndDetectionFrameId()
        {
            FrameSynchronizer synchronizer = new();

            List<SynchronizedFrame> results = new();

            synchronizer.FrameReady += (_, args) => { results.Add(args.Frame); };

            for (uint frameId = 1; frameId <= 100; frameId++)
            {
                if (frameId % 2 == 0)
                {
                    synchronizer.PushDetection(CreateDetectionFrame(frameId));
                    synchronizer.PushVideo(CreateVideoFrame(frameId));
                }
                else
                {
                    synchronizer.PushVideo(CreateVideoFrame(frameId));
                    synchronizer.PushDetection(CreateDetectionFrame(frameId));
                }
            }

            foreach (SynchronizedFrame frame in results.Where(frame => frame.HasDetection))
            {
                Assert.Equal(frame.Video.FrameId, frame.Detection!.FrameId);
            }
        }

        [Fact]
        public void MissingDetection_ShouldReleaseVideoOnNextVideoArrival()
        {
            FrameSynchronizer synchronizer = new(bufferDepth: 1);

            List<SynchronizedFrame> results = new();

            synchronizer.FrameReady += (_, args) => { results.Add(args.Frame); };

            synchronizer.PushVideo(CreateVideoFrame(1));

            Assert.Empty(results);

            synchronizer.PushVideo(CreateVideoFrame(2));

            Assert.Single(results);

            Assert.Equal((uint)1, results[0].FrameId);

            Assert.False(results[0].HasDetection);
        }

        [Fact]
        public void DetectionArrival_ShouldNotTriggerPresentation()
        {
            FrameSynchronizer synchronizer = new(bufferDepth: 1);

            List<SynchronizedFrame> results = new();

            synchronizer.FrameReady += (_, args) => { results.Add(args.Frame); };

            synchronizer.PushVideo(CreateVideoFrame(10));

            synchronizer.PushDetection(CreateDetectionFrame(10));

            Assert.Empty(results);

            synchronizer.PushDetection(CreateDetectionFrame(11));

            Assert.Empty(results);
        }
    }
}