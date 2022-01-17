using FluentAssertions;
using NUnit.Framework;
using System;
using Web.NINAPlugin.History;

namespace Web.NINAPlugin.Test {
    public class SessionHistoryTests {

        [Test]
        public void TestBasic() {
            SessionHistory sut = new SessionHistory(DateTime.Now);
            sut.targets.Count.Should().Be(0);
            sut.id.Should().NotBeNull();

            Target t1 = new Target("t1");
            t1.id.Should().NotBeNull();
            t1.name.Should().Be("t1");
            t1.imageRecords.Count.Should().Be(0);

            ImageRecord r1 = new ImageRecord(TestHelper.GetImageSavedEventArgs(DateTime.Now));
            r1.id.Should().NotBeNull();
            r1.fileName.Should().Be("bar.fits");
            r1.duration.Should().Be(11.0);
            r1.filterName.Should().Be("Foo");
            r1.detectedStars.Should().Be(1234);
            r1.HFR.Should().Be(1.23);

            t1.AddImageRecord(r1);
            t1.imageRecords.Count.Should().Be(1);
            t1.AddImageRecord(new ImageRecord(TestHelper.GetImageSavedEventArgs(DateTime.Now)));
            t1.AddImageRecord(new ImageRecord(TestHelper.GetImageSavedEventArgs(DateTime.Now)));
            t1.imageRecords.Count.Should().Be(3);

            sut.activeTargetId.Should().BeNull();
            sut.AddTarget(t1);
            sut.activeTargetId.Should().Be(t1.id);
            sut.targets.Count.Should().Be(1);
        }

    }
}