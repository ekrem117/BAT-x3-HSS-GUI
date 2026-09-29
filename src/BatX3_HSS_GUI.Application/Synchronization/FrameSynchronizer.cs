using BatX3_HSS_GUI.Domain.Detection;
using BatX3_HSS_GUI.Domain.Synchronization;
using BatX3_HSS_GUI.Domain.Video;

namespace BatX3_HSS_GUI.Application.Synchronization
{
    public sealed class FrameSynchronizer :
        IFrameSynchronizer
    {
        /*
         * Video presentation bir video frame geciktirilir.
         *
         * Presentation cadence yalnızca Video stream tarafından
         * belirlenir. Detection arrival hiçbir zaman presentation
         * tetiklemez.
         */
        public const int DefaultBufferDepth =
            1;

        /*
         * Detection video'dan önce gelebileceği için birkaç
         * exact-match candidate tutulur.
         */
        public const int DefaultDetectionBufferDepth =
            8;

        private readonly object _syncRoot =
            new();

        private readonly int _videoBufferDepth;

        private readonly int _detectionBufferDepth;

        private readonly Dictionary<uint, VideoFrame>
            _videoFrames = new();

        private readonly Dictionary<uint, DetectionFrame>
            _detectionFrames = new();

        private readonly Queue<uint>
            _videoArrivalOrder = new();

        private readonly Queue<uint>
            _detectionArrivalOrder = new();

        private long _matchedFrameCount;

        private long _videoOnlyFrameCount;

        private long _droppedDetectionFrameCount;

        public FrameSynchronizer(
            int bufferDepth = DefaultBufferDepth,
            int detectionBufferDepth = DefaultDetectionBufferDepth)
        {
            if (bufferDepth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bufferDepth),
                    bufferDepth,
                    "Video buffer depth sıfırdan büyük olmalıdır.");
            }

            if (detectionBufferDepth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(detectionBufferDepth),
                    detectionBufferDepth,
                    "Detection buffer depth sıfırdan büyük olmalıdır.");
            }

            _videoBufferDepth =
                bufferDepth;

            _detectionBufferDepth =
                detectionBufferDepth;
        }

        public event EventHandler<SynchronizedFrameReadyEventArgs>?
            FrameReady;

        public long MatchedFrameCount =>
            Interlocked.Read(
                ref _matchedFrameCount);

        public long VideoOnlyFrameCount =>
            Interlocked.Read(
                ref _videoOnlyFrameCount);

        public long DroppedDetectionFrameCount =>
            Interlocked.Read(
                ref _droppedDetectionFrameCount);

        public void PushVideo(
            VideoFrame frame)
        {
            ArgumentNullException.ThrowIfNull(
                frame);

            List<SynchronizedFrame> readyFrames =
                new();

            lock (_syncRoot)
            {
                AddOrReplaceVideo(
                    frame);

                /*
                 * bufferDepth = 1 olduğunda:
                 *
                 * Video N gelir
                 *      ↓
                 * Video N-1 finalize edilir.
                 *
                 * Böylece MATCH ve VIDEO ONLY frame'ler aynı
                 * video-clocked presentation cadence ile çıkar.
                 */
                while (
                    _videoFrames.Count >
                    _videoBufferDepth)
                {
                    VideoFrame? video =
                        RemoveOldestVideo();

                    if (video is null)
                    {
                        break;
                    }

                    if (_detectionFrames.Remove(
                            video.FrameId,
                            out DetectionFrame? detection))
                    {
                        Interlocked.Increment(
                            ref _matchedFrameCount);

                        readyFrames.Add(
                            CreateMatchedFrame(
                                video,
                                detection));
                    }
                    else
                    {
                        Interlocked.Increment(
                            ref _videoOnlyFrameCount);

                        readyFrames.Add(
                            CreateVideoOnlyFrame(
                                video));
                    }
                }
            }

            /*
             * Event callback'leri lock dışında çalıştırıyoruz.
             *
             * Subscriber'ın yavaş veya hatalı olması synchronizer'ın
             * internal buffer lock'unu tutmamalıdır.
             */
            foreach (
                SynchronizedFrame readyFrame
                in readyFrames)
            {
                RaiseFrameReady(
                    readyFrame);
            }
        }

        public void PushDetection(
            DetectionFrame frame)
        {
            ArgumentNullException.ThrowIfNull(
                frame);

            lock (_syncRoot)
            {
                /*
                 * Detection arrival presentation tetiklemez.
                 *
                 * Yalnızca exact frame_id eşleşmesi için buffer'a
                 * alınır.
                 */
                AddOrReplaceDetection(
                    frame);

                while (
                    _detectionFrames.Count >
                    _detectionBufferDepth)
                {
                    RemoveOldestDetection();
                }
            }
        }

        public void Reset()
        {
            lock (_syncRoot)
            {
                _videoFrames.Clear();

                _detectionFrames.Clear();

                _videoArrivalOrder.Clear();

                _detectionArrivalOrder.Clear();
            }

            /*
             * Diagnostic sayaçları Reset ile sıfırlanmıyor.
             *
             * Bunlar application session toplamını temsil ediyor.
             */
        }

        private void AddOrReplaceVideo(
            VideoFrame frame)
        {
            if (_videoFrames.ContainsKey(
                    frame.FrameId))
            {
                _videoFrames[frame.FrameId] =
                    frame;

                return;
            }

            _videoFrames.Add(
                frame.FrameId,
                frame);

            _videoArrivalOrder.Enqueue(
                frame.FrameId);
        }

        private void AddOrReplaceDetection(
            DetectionFrame frame)
        {
            if (_detectionFrames.ContainsKey(
                    frame.FrameId))
            {
                _detectionFrames[frame.FrameId] =
                    frame;

                return;
            }

            _detectionFrames.Add(
                frame.FrameId,
                frame);

            _detectionArrivalOrder.Enqueue(
                frame.FrameId);
        }

        private VideoFrame? RemoveOldestVideo()
        {
            while (_videoArrivalOrder.Count > 0)
            {
                uint frameId =
                    _videoArrivalOrder.Dequeue();

                if (_videoFrames.Remove(
                        frameId,
                        out VideoFrame? frame))
                {
                    return frame;
                }
            }

            return null;
        }

        private void RemoveOldestDetection()
        {
            while (_detectionArrivalOrder.Count > 0)
            {
                uint frameId =
                    _detectionArrivalOrder.Dequeue();

                if (_detectionFrames.Remove(
                        frameId))
                {
                    Interlocked.Increment(
                        ref _droppedDetectionFrameCount);

                    return;
                }
            }
        }

        private static SynchronizedFrame CreateMatchedFrame(
            VideoFrame video,
            DetectionFrame detection)
        {
            if (video.FrameId != detection.FrameId)
            {
                throw new InvalidOperationException(
                    $"Frame ID uyuşmazlığı. " +
                    $"Video={video.FrameId}, " +
                    $"Detection={detection.FrameId}");
            }

            return new SynchronizedFrame
            {
                Video =
                    video,

                Detection =
                    detection
            };
        }

        private static SynchronizedFrame CreateVideoOnlyFrame(
            VideoFrame video)
        {
            return new SynchronizedFrame
            {
                Video =
                    video,

                Detection =
                    null
            };
        }

        private void RaiseFrameReady(
            SynchronizedFrame frame)
        {
            FrameReady?.Invoke(
                this,
                new SynchronizedFrameReadyEventArgs(
                    frame));
        }
    }
}