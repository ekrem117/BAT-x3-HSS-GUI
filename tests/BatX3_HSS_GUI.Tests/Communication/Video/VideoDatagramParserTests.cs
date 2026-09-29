using BatX3_HSS_GUI.Domain.Video;
using BatX3_HSS_GUI.Infrastructure.Communication.Video;
using System.Buffers.Binary;

namespace BatX3_HSS_GUI.Tests.Communication.Video
{
    public sealed class VideoDatagramParserTests
    {
        [Fact]
        public void TryParse_ValidDatagram_ShouldReturnFrame()
        {
            byte[] jpeg =
            [
                0xFF,
                0xD8,
                0xFF,
                0xD9
            ];

            byte[] datagram =
                CreateDatagram(
                    frameId: 42,
                    timestamp: 1234.5,
                    width: 1280,
                    height: 720,
                    jpeg: jpeg);

            bool success = VideoDatagramParser.TryParse(datagram, out VideoFrame? frame, out string? error);

            Assert.True(success);
            Assert.Null(error);
            Assert.NotNull(frame);

            Assert.Equal((uint)42, frame.FrameId);

            Assert.Equal(1234.5, frame.Timestamp);

            Assert.Equal((ushort)1280, frame.Width);

            Assert.Equal((ushort)720, frame.Height);

            Assert.Equal(jpeg, frame.JpegBytes);
        }

        [Fact]
        public void TryParse_InvalidMagic_ShouldFail()
        {
            byte[] datagram =
                CreateDatagram(
                    frameId: 1,
                    timestamp: 1.0,
                    width: 1280,
                    height: 720,
                    jpeg:
                    [
                        0xFF,
                        0xD8,
                        0xFF,
                        0xD9
                    ]);

            datagram[0] = (byte)'X';

            bool success = VideoDatagramParser.TryParse(datagram, out VideoFrame? frame, out string? error);

            Assert.False(success);
            Assert.Null(frame);
            Assert.NotNull(error);
        }

        [Fact]
        public void TryParse_JpegSizeMismatch_ShouldFail()
        {
            byte[] datagram =
                CreateDatagram(
                    frameId: 1,
                    timestamp: 1.0,
                    width: 1280,
                    height: 720,
                    jpeg:
                    [
                        0xFF,
                        0xD8,
                        0xFF,
                        0xD9
                    ]);

            BinaryPrimitives.WriteUInt32LittleEndian(datagram.AsSpan(16, 4), 999);

            bool success = VideoDatagramParser.TryParse(datagram, out VideoFrame? frame, out string? error);

            Assert.False(success);
            Assert.Null(frame);
            Assert.NotNull(error);
        }

        private static byte[] CreateDatagram(
            uint frameId,
            double timestamp,
            ushort width,
            ushort height,
            byte[] jpeg)
        {
            byte[] datagram = new byte[VideoDatagramParser.HeaderSize + jpeg.Length];

            datagram[0] = (byte)'B';

            datagram[1] = (byte)'X';

            datagram[2] = (byte)'3';

            datagram[3] = (byte)'F';

            BinaryPrimitives.WriteUInt32LittleEndian(datagram.AsSpan(4, 4), frameId);

            BinaryPrimitives.WriteInt64LittleEndian(datagram.AsSpan(8, 8), BitConverter.DoubleToInt64Bits(timestamp));

            BinaryPrimitives.WriteUInt32LittleEndian(datagram.AsSpan(16, 4), (uint)jpeg.Length);

            BinaryPrimitives.WriteUInt16LittleEndian(datagram.AsSpan(20, 2), width);

            BinaryPrimitives.WriteUInt16LittleEndian(datagram.AsSpan(22, 2), height);

            jpeg.CopyTo(datagram, VideoDatagramParser.HeaderSize);

            return datagram;
        }
    }
}