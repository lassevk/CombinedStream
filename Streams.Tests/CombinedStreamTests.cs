using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Constraints;

// ReSharper disable ObjectCreationAsStatement
// ReSharper disable PossibleNullReferenceException

namespace Streams.Tests
{
    [TestFixture]
    public class CombinedStreamTests
    {
        private static Stream MemStreamOfBytes(params byte[] bytes)
        {
            return new MemoryStream(bytes);
        }

        [Test]
        public void Constructor_NullUnderlyingStreams_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CombinedStream(null));
        }

        [Test]
        public void LengthShouldBeSumOfLengthsOfUnderlyingStreams()
        {
            var us1 = MemStreamOfBytes(0x1);
            var us2 = MemStreamOfBytes(0x1, 0x2);
            var us3 = MemStreamOfBytes(0x1, 0x2, 0x3);

            var cs = new CombinedStream(us1, us2, us3);
            Assert.That(cs.Length, Is.EqualTo(us1.Length + us2.Length + us3.Length), "Length");
        }

        [Test]
        public void ReadWholeStream()
        {
            var us1 = MemStreamOfBytes(0x1);
            var us2 = MemStreamOfBytes(0x2);
            var us3 = MemStreamOfBytes(0x3);

            var cs = new CombinedStream(us1, us2, us3);
            var buf = new byte[3];
            Assert.That(cs.Read(buf, 0, 3), Is.EqualTo(3), "Read");
            Assert.That(buf, Is.EqualTo(new[]{ 0x1, 0x2, 0x3 }), "readed buf");
        }

        [Test]
        public void CanSeekToEnd()
        {
            var us1 = MemStreamOfBytes(0x1);
            var us2 = MemStreamOfBytes(0x2);
            var us3 = MemStreamOfBytes(0x3);

            var cs = new CombinedStream(us1, us2, us3);
            Assert.That(cs.Seek(0, SeekOrigin.End), Is.EqualTo(3), "Seek");
            Assert.That(cs.Position, Is.EqualTo(3), "Position");
        }

        [Test]
        [TestCase(0, SeekOrigin.Begin, 0)]
        [TestCase(1, SeekOrigin.Begin, 1)]
        [TestCase(2, SeekOrigin.Begin, 2)]
        [TestCase(3, SeekOrigin.Begin, 3)]
        [TestCase(4, SeekOrigin.Begin, 4)]
        [TestCase(5, SeekOrigin.Begin, 5)]
        [TestCase(0, SeekOrigin.End, 5)]
        [TestCase(-1, SeekOrigin.End, 4)]
        [TestCase(-5, SeekOrigin.End, 0)]
        public void CanSeekToAllPositionsAcrossMultipleStreams(int offset, SeekOrigin origin, int expectedPosition)
        {
            var streams = Enumerable.Range(0, 5).Select(_ => MemStreamOfBytes(0x00)).ToArray();
            var combined = new CombinedStream(streams);

            combined.Seek(offset, origin);
            Assert.That(combined.Position, Is.EqualTo(expectedPosition));
        }

        [Test]
        public void CantSeekBeforeBeginOfFirstUnderlyingStream()
        {
            var throws = new Func<IResolveConstraint>(() => Throws.Exception.TypeOf<IOException>().With.Message.EqualTo("An attempt was made to move the position before the beginning of the stream."));

            var us1 = MemStreamOfBytes(0x1);
            var us2 = MemStreamOfBytes(0x2);
            var us3 = MemStreamOfBytes(0x3);
            var cs = new CombinedStream(us1, us2, us3);

            // seek from Begin
            Assert.That(() => cs.Seek(-1, SeekOrigin.Begin), throws());
            Assert.That(cs.Position, Is.EqualTo(0));

            // seek from Current
            cs.Seek(1, SeekOrigin.Begin);
            Assert.That(() => cs.Seek(-2, SeekOrigin.Current), throws());
            Assert.That(cs.Position, Is.EqualTo(1));

            // seek from End
            Assert.That(() => cs.Seek(-4, SeekOrigin.End), throws());
            Assert.That(cs.Position, Is.EqualTo(1));
        }
    }
}
