using System;

public static class TimeSpanAccurate {
    // For constants defined below, assume 1 year = 365.25 days
    // 1 century = 100 years
    public const long TicksPerCentury = TimeSpan.TicksPerDay * 36525L;
    public const long TicksPerYear = TimeSpan.TicksPerHour * 8766L;
    // 1 year = 12 identical length months => Average 30.4375 days per month
    public const long TicksPerMonth = TimeSpan.TicksPerMinute * 43830L;
    // 1 week = 7 days
    public const long TicksPerWeek = TimeSpan.TicksPerDay * 7L;
    // 1 nano second = 100 ticks
    public const long TicksPerNanoSecond = 100L;

    public static TimeSpan FromCentury(double century) {
        return new TimeSpan((long)Math.Round(century * TicksPerCentury));
    }

    public static TimeSpan FromYear(double year) {
        return new TimeSpan((long)Math.Round(year * TicksPerYear));
    }

    public static TimeSpan FromMonth(double month) {
        return new TimeSpan((long)Math.Round(month * TicksPerMonth));
    }

    public static TimeSpan FromWeek(double week) {
        return new TimeSpan((long)Math.Round(week * TicksPerWeek));
    }

    public static TimeSpan FromDay(double day) {
        return new TimeSpan((long)Math.Round(day * TimeSpan.TicksPerDay));
    }

    public static TimeSpan FromHour(double hour) {
        return new TimeSpan((long)Math.Round(hour * TimeSpan.TicksPerHour));
    }

    public static TimeSpan FromMinute(double minute) {
        return new TimeSpan((long)Math.Round(minute * TimeSpan.TicksPerMinute));
    }

    public static TimeSpan FromSecond(double second) {
        return new TimeSpan((long)Math.Round(second * TimeSpan.TicksPerSecond));
    }

    public static TimeSpan FromMillisecond(double millisecond) {
        return new TimeSpan((long)Math.Round(millisecond * TimeSpan.TicksPerMillisecond));
    }

    public static TimeSpan FromNanosecond(double nanosecond) {
        return new TimeSpan((long)Math.Round(nanosecond * TicksPerNanoSecond));
    }

    public static double ToAccurateCentury(this TimeSpan timeSpan) {
        return (double)timeSpan.Ticks / TicksPerCentury;
    }

    public static double ToAccurateYear(this TimeSpan timeSpan) {
        return (double)timeSpan.Ticks / TicksPerYear;
    }

    public static double ToAccurateMonth(this TimeSpan timeSpan) {
        return (double)timeSpan.Ticks / TicksPerMonth;
    }

    public static double ToAccurateWeek(this TimeSpan timeSpan) {
        return (double)timeSpan.Ticks / TicksPerWeek;
    }

    public static double ToAccurateDay(this TimeSpan timeSpan) {
        return (double)timeSpan.Ticks / TimeSpan.TicksPerDay;
    }

    public static double ToAccurateHour(this TimeSpan timeSpan) {
        return (double)timeSpan.Ticks / TimeSpan.TicksPerHour;
    }

    public static double ToAccurateMinute(this TimeSpan timeSpan) {
        return (double)timeSpan.Ticks / TimeSpan.TicksPerMinute;
    }

    public static double ToAccurateSecond(this TimeSpan timeSpan) {
        return (double)timeSpan.Ticks / TimeSpan.TicksPerSecond;
    }

    public static double ToAccurateMillisecond(this TimeSpan timeSpan) {
        return (double)timeSpan.Ticks / TimeSpan.TicksPerMillisecond;
    }

    public static double ToAccurateNanosecond(this TimeSpan timeSpan) {
        return (double)timeSpan.Ticks / TicksPerNanoSecond;
    }

    public static float ToAccurateCenturyF(this TimeSpan timeSpan) {
        return (float)timeSpan.Ticks / TicksPerCentury;
    }

    public static float ToAccurateYearF(this TimeSpan timeSpan) {
        return (float)timeSpan.Ticks / TicksPerYear;
    }

    public static float ToAccurateMonthF(this TimeSpan timeSpan) {
        return (float)timeSpan.Ticks / TicksPerMonth;
    }

    public static float ToAccurateWeekF(this TimeSpan timeSpan) {
        return (float)timeSpan.Ticks / TicksPerWeek;
    }

    public static float ToAccurateDayF(this TimeSpan timeSpan) {
        return (float)timeSpan.Ticks / TimeSpan.TicksPerDay;
    }

    public static float ToAccurateHourF(this TimeSpan timeSpan) {
        return (float)timeSpan.Ticks / TimeSpan.TicksPerHour;
    }

    public static float ToAccurateMinuteF(this TimeSpan timeSpan) {
        return (float)timeSpan.Ticks / TimeSpan.TicksPerMinute;
    }

    public static float ToAccurateSecondF(this TimeSpan timeSpan) {
        return (float)timeSpan.Ticks / TimeSpan.TicksPerSecond;
    }

    public static float ToAccurateMillisecondF(this TimeSpan timeSpan) {
        return (float)timeSpan.Ticks / TimeSpan.TicksPerMillisecond;
    }

    public static float ToAccurateNanosecondF(this TimeSpan timeSpan) {
        return (float)timeSpan.Ticks / TicksPerNanoSecond;
    }
}

public static class DateTimeHelper {
    const long maxTicks = 3155378975999999999L;

    const long epoch0Ticks = 621355968000000000L;
    const long minUnixTime = -62135596800L;
    const long maxUnixTime = 253402300800L;

    public static long ToUnixTime(this DateTime source) {
        if(source.Kind == DateTimeKind.Local)
            source = source.ToUniversalTime();
        return (source.Ticks - epoch0Ticks) / TimeSpan.TicksPerSecond;
    }

    public static double ToUnixTimeFloat(this DateTime source) {
        if(source.Kind == DateTimeKind.Local)
            source = source.ToUniversalTime();
        return (double)(source.Ticks - epoch0Ticks) / TimeSpan.TicksPerSecond;
    }

    public static DateTime FromUnixTime(long unixTime) {
        if(unixTime < minUnixTime || unixTime > maxUnixTime)
            throw new ArgumentOutOfRangeException(
                "unixTime",
                unixTime,
                string.Format("The input unix time is out of supported range (between {0} and {1}, provided {2}).", minUnixTime, maxUnixTime, unixTime)
            );
        return new DateTime(epoch0Ticks + unixTime * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
    }

    public static DateTime FromUnixTime(double unixTime) {
        if(unixTime < minUnixTime || unixTime > maxUnixTime)
            throw new ArgumentOutOfRangeException(
                "unixTime",
                unixTime,
                string.Format("The input unix time is out of supported range (between {0} and {1}, provided {2}).", minUnixTime, maxUnixTime, unixTime)
            );
        long ticks = epoch0Ticks + (long)Math.Round(unixTime * TimeSpan.TicksPerSecond);
        if(ticks > maxTicks) ticks = maxTicks;
        else if(ticks < 0) ticks = 0;
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}