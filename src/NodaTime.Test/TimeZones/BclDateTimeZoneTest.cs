// Copyright 2012 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

#if !PCL

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;
using NodaTime.TimeZones;

namespace NodaTime.Test.TimeZones
{
    [TestFixture]
    public class BclDateTimeZoneTest
    {
#pragma warning disable 0414 // Used by tests via reflection - do not remove!
        // This test is effectively disabled on Mono as its time zone support is broken in the current
        // stable release - see https://github.com/nodatime/nodatime/issues/97
        private static readonly ReadOnlyCollection<TimeZoneInfo> BclZonesOrEmptyOnMono = TestHelper.IsRunningOnMono
            ? new List<TimeZoneInfo>().AsReadOnly() : TimeZoneInfo.GetSystemTimeZones();
#pragma warning restore 0414

        [Test]
        [TestCaseSource("BclZonesOrEmptyOnMono")]
        public void AllZoneTransitions(TimeZoneInfo windowsZone)
        {
            var nodaZone = BclDateTimeZone.FromTimeZoneInfo(windowsZone);

            Instant instant = Instant.FromUtc(1800, 1, 1, 0, 0);
            Instant end = Instant.FromUtc(2050, 1, 1, 0, 0);

            while (instant < end)
            {
                ValidateZoneEquality(instant - Duration.Epsilon, nodaZone, windowsZone);
                ValidateZoneEquality(instant, nodaZone, windowsZone);
                instant = nodaZone.GetZoneInterval(instant).RawEnd;
            }
        }

        /// <summary>
        /// This test catches situations where the Noda Time representation doesn't have all the
        /// transitions it should; AllZoneTransitions may pass not spot times when we *should* have
        /// a transition, because it only uses the transitions it knows about. Instead, here we
        /// check each day between 1st January 1950 and 1st January 2050. We use midnight UTC, but
        /// this is arbitrary. The choice of checking once a week is just practical - it's a relatively
        /// slow test, mostly because TimeZoneInfo is slow.
        /// </summary>
        [Test]
        [TestCaseSource("BclZonesOrEmptyOnMono")]
        public void AllZonesEveryWeek(TimeZoneInfo windowsZone)
        {
            ValidateZoneEveryWeek(windowsZone);
        }

        // This demonstrates bug 115.
        [Test]
        public void Namibia()
        {
            String bclId = "Namibia Standard Time";
            try
            {
                ValidateZoneEveryWeek(TimeZoneInfo.FindSystemTimeZoneById(bclId));
            }
            catch (TimeZoneNotFoundException)
            {
                // This may occur on Mono, for example.
                Assert.Ignore("Test assumes existence of BCL zone with ID: " + bclId);
            }
        }

        [Test]
        [TestCaseSource("BclZonesOrEmptyOnMono")]
        public void AllZonesStartAndEndOfTime(TimeZoneInfo windowsZone)
        {
            var nodaZone = BclDateTimeZone.FromTimeZoneInfo(windowsZone);
            var firstInterval = nodaZone.GetZoneInterval(Instant.MinValue);
            Assert.IsFalse(firstInterval.HasStart);
            var lastInterval = nodaZone.GetZoneInterval(Instant.MaxValue);
            Assert.IsFalse(lastInterval.HasEnd);
        }

        private void ValidateZoneEveryWeek(TimeZoneInfo windowsZone)
        {
            var nodaZone = BclDateTimeZone.FromTimeZoneInfo(windowsZone);

            Instant instant = Instant.FromUtc(1950, 1, 1, 0, 0);
            Instant end = Instant.FromUtc(2050, 1, 1, 0, 0);

            while (instant < end)
            {
                ValidateZoneEquality(instant, nodaZone, windowsZone);
                instant += Duration.OneWeek;
            }
        }

        [Test]
        public void ForSystemDefault()
        {
            // Assume that the local time zone doesn't change between two calls...
            TimeZoneInfo local = TimeZoneInfo.Local;
            BclDateTimeZone nodaLocal1 = BclDateTimeZone.ForSystemDefault();
            BclDateTimeZone nodaLocal2 = BclDateTimeZone.ForSystemDefault();
            // Check it's actually the right zone
            Assert.AreSame(local, nodaLocal1.OriginalZone);
            // Check it's cached
            Assert.AreSame(nodaLocal1, nodaLocal2);
        }

        [Test]
        public void DateTimeMinValueStartRuleExtendsToBeginningOfTime()
        {
            var rules = new[]
            {
                // Rule for the whole of time, with DST of 1 hour commencing on March 1st
                // and ending on September 1st.
                TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                    DateTime.MinValue, DateTime.MaxValue.Date, TimeSpan.FromHours(1),
                    TimeZoneInfo.TransitionTime.CreateFixedDateRule(DateTime.MinValue, 3, 1),
                    TimeZoneInfo.TransitionTime.CreateFixedDateRule(DateTime.MinValue, 9, 1))
            };
            var bclZone = TimeZoneInfo.CreateCustomTimeZone("custom", baseUtcOffset: TimeSpan.Zero,
                displayName: "DisplayName", standardDisplayName: "Standard",
                daylightDisplayName: "Daylight",
                adjustmentRules: rules);
            var nodaZone = BclDateTimeZone.FromTimeZoneInfo(bclZone);
            // Standard time in February BC 101
            Assert.AreEqual(Offset.Zero, nodaZone.GetUtcOffset(Instant.FromUtc(-100, 2, 1, 0, 0)));
            // Daylight time in July BC 101
            Assert.AreEqual(Offset.FromHours(1), nodaZone.GetUtcOffset(Instant.FromUtc(-100, 7, 1, 0, 0)));
            // Standard time in October BC 101
            Assert.AreEqual(Offset.Zero, nodaZone.GetUtcOffset(Instant.FromUtc(-100, 10, 1, 0, 0)));
        }

        [Test]
        public void Equality()
        {
            if (BclZonesOrEmptyOnMono.Count < 2)
            {
                return;
            }
            var firstEqual = BclDateTimeZone.FromTimeZoneInfo(BclZonesOrEmptyOnMono[0]);
            var secondEqual = BclDateTimeZone.FromTimeZoneInfo(BclZonesOrEmptyOnMono[0]);
            var unequal = BclDateTimeZone.FromTimeZoneInfo(BclZonesOrEmptyOnMono[1]);
            Assert.AreEqual(firstEqual, secondEqual);
            Assert.AreEqual(firstEqual.GetHashCode(), secondEqual.GetHashCode());
            Assert.AreNotSame(firstEqual, secondEqual);
            Assert.AreNotEqual(firstEqual, unequal);
        }

        private void ValidateZoneEquality(Instant instant, DateTimeZone nodaZone, TimeZoneInfo windowsZone)
        {
            // The BCL is basically broken (up to and including .NET 4.5.1 at least) around its interpretation
            // of its own data around the new year. See http://codeblog.jonskeet.uk/2014/09/30/the-mysteries-of-bcl-time-zone-data/
            // for details. We're not trying to emulate this behaviour.
            // It's a lot *better* for .NET 4.6, 
            var utc = instant.InUtc();
            if ((utc.Month == 12 && utc.Day == 31) || (utc.Month == 1 && utc.Day == 1))
            {
                return;
            }

            var interval = nodaZone.GetZoneInterval(instant);

            // Check that the zone interval really represents a transition. It could be a change in
            // wall offset, name, or the split between standard time and daylight savings for the interval.
            if (interval.RawStart != Instant.BeforeMinValue)
            {
                var previousInterval = nodaZone.GetZoneInterval(interval.Start - Duration.Epsilon);
                Assert.AreNotEqual(new {interval.WallOffset, interval.Name, interval.StandardOffset},
                    new {previousInterval.WallOffset, previousInterval.Name, previousInterval.StandardOffset},
                    "Non-transition from {0} to {1}", previousInterval, interval);
            }
            var nodaOffset = interval.WallOffset;
            var windowsOffset = windowsZone.GetUtcOffset(instant.ToDateTimeUtc());
            Assert.AreEqual(windowsOffset, nodaOffset.ToTimeSpan(), $"Incorrect offset at {instant} in interval {interval}");
            var bclDaylight = windowsZone.IsDaylightSavingTime(instant.ToDateTimeUtc());
            Assert.AreEqual(bclDaylight, interval.Savings != Offset.Zero,
                $"At {instant}, BCL IsDaylightSavingTime={bclDaylight}; Noda savings={interval.Savings}");
        }
    }
}

#endif
