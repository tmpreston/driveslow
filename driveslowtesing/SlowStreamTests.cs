using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using driveslow.core;
using NUnit.Framework;

namespace driveslowtesting
{
    [TestFixture]
    public class SlowStreamTests
    {

        private readonly string Temp100kFile = "Temp.100k.tmp";
        private readonly string Temp1MBFile = "Temp.1MB.tmp";
        private Stopwatch timing = new Stopwatch();
        [SetUp]
        public void SetUp()
        {
            using (var new100kFile = new FileStream(Temp100kFile, FileMode.Create))
            {
                new100kFile.Write(new byte[100*1024], 0, 100*1024);
            }
            using (var new1MBFile = new FileStream(Temp1MBFile, FileMode.Create))
            {
                new1MBFile.Write(new byte[10*100*1024], 0, 10*100*1024);
            }
            timing = new Stopwatch();
        }

        [TearDown]
        public void TearDown()
        {
            timing = null;
        }

        private void PerformTestAtRate(int rateInKBs, string tempFileToUse)
        {
            timing.Restart();
            int bytesRead = 0;
            using (var file = new SlowStream(new FileStream(tempFileToUse, FileMode.Open, FileAccess.Read), rateInKBs))
            {
                var buffer = new byte[4096];
                do
                {
                    bytesRead += file.Read(buffer, 0, buffer.Length);
                } while (file.CanRead && file.Position != file.Length && bytesRead < file.Length);
            }
            var time = timing.ElapsedMilliseconds;
            var kbBytes = bytesRead/1024.0;
            var msTime = time/1000.0;
            System.Console.WriteLine("{0}kb in {1:0.00}s == {2:0.00} kb/s", kbBytes, msTime, kbBytes/msTime);
            Thread.Sleep(1000);
        }

        [Test]
        public void InitialReadTest_1MB()
        {
            PerformTestAtRate(5, Temp1MBFile);
        }

        [Test]
        public void InitialReadTest_100k()
        {
            PerformTestAtRate(5, Temp100kFile);
        }

        [Test, Explicit]
        public void TestMultipleRates()
        {
            var ratesToTest = new[]
            {
                100,
                80,
                50,
                20,
                15,
                10,
                5,
                2,
            }.ToList();
            ratesToTest.ForEach(zz =>
            {
                Console.WriteLine("Perform test at : {0} kb/s", zz);
                PerformTestAtRate(zz, Temp1MBFile);
            });
        }
    }
}