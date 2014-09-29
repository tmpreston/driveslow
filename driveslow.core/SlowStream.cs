using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace driveslow.core
{
    /// <summary>
    /// This class provides a slower wrapper around an underlying stream.
    /// </summary>
    public class SlowStream : Stream
    {
        private Stream baseStream;
        private Stopwatch swTimer;
        /// <summary>
        /// Time slice to use  (ms)
        /// </summary>
        private const int TimeSlice=100;
        private readonly int rate;
        private readonly int bytesPerTimeSlice;

        private int totalBytesRead = 0;

        public SlowStream(Stream baseStreamToUse, int kbPerSecond)
        {
            if (baseStreamToUse == null) throw new ArgumentNullException("baseStreamToUse");
            if(kbPerSecond == 0) throw new ArgumentException("Requires a value > 0. (kB/sec)", "kbPerSecond");
            baseStream = baseStreamToUse;
            rate = kbPerSecond*1024;
            bytesPerTimeSlice = rate/(1000/TimeSlice);
            swTimer = new Stopwatch();
            swTimer.Start();
        }

        public new void Dispose()
        {
            swTimer.Stop();
            baseStream.Dispose();
            base.Dispose();
        }

        public override void Flush()
        {
            baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            baseStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var startOfSlice = swTimer.ElapsedMilliseconds;
            var maxCountToRead = Math.Min(bytesPerTimeSlice, count);
            var bytesRead = baseStream.Read(buffer, offset, maxCountToRead);
            totalBytesRead += bytesRead;
            var difference = swTimer.ElapsedMilliseconds - startOfSlice;
            var totalElapsed = swTimer.ElapsedMilliseconds;
            if (totalElapsed == 0)
            {
                totalElapsed = 1;
            }
            var currentRate = totalBytesRead*1.0/totalElapsed*1000;
            bool isBelowDesiredRate = currentRate < rate*0.90;
            bool isAboveDesiredRate = currentRate > rate*1.10;
            if (isAboveDesiredRate)
            {
                //Sleep for partial timeslice if going to fast.
                Thread.Sleep(TimeSlice/4);
            }

            if (isBelowDesiredRate)
            {
                //Dont sleep if below current rate.
            }
            else if (difference == 0)
            {
                //Sleep for timeslice.
                Thread.Sleep(TimeSlice);
            }
            else if (difference > 0 && difference < TimeSlice)
            {
                //Sleep remainder of timeslice.
                Thread.Sleep(TimeSlice - (int) difference);
            }
            //Else already taken too long
            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            baseStream.Write(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return baseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return baseStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return baseStream.CanWrite; }
        }

        public override long Length
        {
            get { return baseStream.Length; }
        }

        public override long Position
        {
            get { return baseStream.Position; }
            set { baseStream.Position = value; }
        }
    }
}