using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using Web.NINAPlugin.History;

namespace Web.NINAPlugin.Test {

    public class SessionHistoryManagerTest {

        private string tempDir;

        [SetUp]
        public void SetUp() {
            tempDir = Path.Combine(Path.GetTempPath(), "SHM_test");
            Directory.CreateDirectory(tempDir);
        }

        [Test]
        public void TestBasic() {
            SessionHistory sh = new SessionHistory(DateTime.Now);
            Target t1 = new Target("t1");

            t1.AddImageRecord(new ImageRecord(TestHelper.GetImageSavedEventArgs(DateTime.Now)));
            t1.AddImageRecord(new ImageRecord(TestHelper.GetImageSavedEventArgs(DateTime.Now)));
            t1.AddImageRecord(new ImageRecord(TestHelper.GetImageSavedEventArgs(DateTime.Now)));

            sh.AddTarget(t1);

            SessionHistoryManager sut = new SessionHistoryManager(tempDir);
            string sessionHome = sut.CreateOrUpdateSessionHistory(sh);
            Console.WriteLine(sessionHome);

            SessionHistory sh2 = sut.GetSessionHistory(sessionHome);
            sh2.id.Should().Be(sh.id);
            sh2.activeTargetId.Should().Be(sh.activeTargetId);
        }

        [Test]
        public void TestBad() {
            Action act = () => new SessionHistoryManager(null);
            act.Should().Throw<Exception>().WithMessage("root directory for session manager cannot be null/empty");
            act = () => new SessionHistoryManager("");
            act.Should().Throw<Exception>().WithMessage("root directory for session manager cannot be null/empty");

            act = () => new SessionHistoryManager("nada");
            act.Should().Throw<Exception>().WithMessage("root directory for session manager must exist*");
        }

        [TearDown]
        public void TearDown() {
            if (tempDir != null && Directory.Exists(tempDir)) {
                Directory.Delete(tempDir, true);
            }
        }

    }
}
