using BatX3_HSS_GUI.Domain.Video;
using System.Buffers.Binary;

namespace BatX3_HSS_GUI.Infrastructure.Communication.Video
{
    internal static class VideoDatagramParser
    {
        public const int HeaderSize = 24;

        private static ReadOnlySpan<byte> Magic =>
        [
            (byte)'B',
            (byte)'X',
            (byte)'3',
            (byte)'F'
        ];

        public static bool TryParse(ReadOnlySpan<byte> datagram, out VideoFrame? frame, out string? error)
        {
            frame = null;
            error = null;

            if (datagram.Length < HeaderSize)
            {
                error = $"Video datagram header'dan kısa. " + $"Length={datagram.Length}";
                return false;
            }

            if (!datagram[..4].SequenceEqual(Magic))
            {
                error = "Geçersiz video magic. BX3F bekleniyor.";
                return false;
            }

            uint frameId = BinaryPrimitives.ReadUInt32LittleEndian(datagram.Slice(4, 4));

            long timestampBits = BinaryPrimitives.ReadInt64LittleEndian(datagram.Slice(8, 8));

            double timestamp = BitConverter.Int64BitsToDouble(timestampBits);

            uint jpegSize = BinaryPrimitives.ReadUInt32LittleEndian(datagram.Slice(16, 4));

            ushort width = BinaryPrimitives.ReadUInt16LittleEndian(datagram.Slice(20, 2));

            ushort height = BinaryPrimitives.ReadUInt16LittleEndian(datagram.Slice(22, 2));

            int payloadLength = datagram.Length - HeaderSize;

            if (jpegSize != (uint)payloadLength)
            {
                error = $"JPEG boyutu uyuşmuyor. " + $"Header={jpegSize}, Payload={payloadLength}";
                return false;
            }

            if (jpegSize == 0)
            {
                error = "Video frame JPEG payload boş.";
                return false;
            }

            if (width == 0 || height == 0)
            {
                error = $"Geçersiz frame çözünürlüğü: {width}x{height}";
                return false;
            }

            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp))
            {
                error = "Geçersiz video timestamp.";
                return false;
            }

            byte[] jpegBytes = datagram.Slice(HeaderSize, payloadLength).ToArray();

            frame = new VideoFrame
            {
                FrameId = frameId,
                Timestamp = timestamp,
                Width = width,
                Height = height,
                JpegBytes = jpegBytes
            };

            return true;
        }
    }
}