using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Running;

namespace ddd
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<DateTimeStuff>();

        }
    }

    public static class DateTimeExtensions
    {
        // Number of 100ns ticks per time unit
        const long TicksPerMillisecond = 10000;
        const long TicksPerSecond = TicksPerMillisecond * 1000;
        const long TicksPerMinute = TicksPerSecond * 60;
        const long TicksPerHour = TicksPerMinute * 60;
        const long TicksPerDay = TicksPerHour * 24;

        // Number of milliseconds per time unit
        const int MillisPerSecond = 1000;
        const int MillisPerMinute = MillisPerSecond * 60;
        const int MillisPerHour = MillisPerMinute * 60;
        const int MillisPerDay = MillisPerHour * 24;

        // Number of days in a non-leap year
        const int DaysPerYear = 365;
        // Number of days in 4 years
        const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
        // Number of days in 100 years
        const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
        // Number of days in 400 years
        const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097

        // Number of days from 1/1/0001 to 12/31/1600
        const int DaysTo1601 = DaysPer400Years * 4;          // 584388
        // Number of days from 1/1/0001 to 12/30/1899
        const int DaysTo1899 = DaysPer400Years * 4 + DaysPer100Years * 3 - 367;
        // Number of days from 1/1/0001 to 12/31/1969
        internal const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear; // 719,162
        // Number of days from 1/1/0001 to 12/31/9999
        const int DaysTo10000 = DaysPer400Years * 25 - 366;  // 3652059

        static readonly int[] DaysToMonth365 = {
            0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365};

        static readonly int[] DaysToMonth366 = {
            0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366};

     #if INSANE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Deconstruct(this DateTime dt, out int year, out int month, out int day)
        {
            var ticks = dt.Ticks;
            // n = number of days since 1/1/0001
            var n = (int)(ticks / TicksPerDay);
            // y400 = number of whole 400-year periods since 1/1/0001
            //var y400 = n / DaysPer400Years;
            var y400 = (int) ((n * 963315389L) >> 47);
            // n = day number within 400-year period
            n -= y400 * DaysPer400Years;
            // y100 = number of whole 100-year periods within 400-year period
            //var y100 = n / DaysPer100Years;
            var y100 = (int) ((n * 3853287931L) >> 47);
            // Last 100-year period has an extra day, so decrement result if 4
            if (y100 == 4) y100 = 3;
            // n = day number within 100-year period
            n -= y100 * DaysPer100Years;
            // y4 = number of whole 4-year periods within 100-year period
            //var y4 = n / DaysPer4Years;
            var y4 = (int) ((n * 376287347L) >> 39);
            // n = day number within 4-year period
            n -= y4 * DaysPer4Years;
            // y1 = number of whole years within 4-year period
            //var y1 = n / DaysPerYear;
            var y1 = (int) ((n * 3012360625L) >> 40);
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4)
                y1 = 3;
            // If year was requested, compute and return it
            year = y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;
            // n = day number within year
            n -= y1 * DaysPerYear;
            // Leap year calculation looks different from IsLeapYear since y1, y4,
            // and y100 are relative to year 1, not year 0
            var leapYear = y1 == 3 && (y4 != 24 || y100 == 3);
            var days = leapYear? DaysToMonth366: DaysToMonth365;
            // All months have less than 32 days, so n >> 5 is a good conservative
            // estimate for the month
            var m = n >> 5 + 1;
            // m = 1-based month number
            while (n >= days[m]) m++;
            // If month was requested, return it
            month = m;
            // Return 1-based day-of-month
            day = n - days[m - 1] + 1;
        }
    #else        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Deconstruct(this DateTime dt, out int year, out int month, out int day)
        {
            var ticks = dt.Ticks;
            // n = number of days since 1/1/0001
            var n = (int)(ticks / TicksPerDay);
            // y400 = number of whole 400-year periods since 1/1/0001
            var y400 = n / DaysPer400Years;
            // n = day number within 400-year period
            n -= y400 * DaysPer400Years;
            // y100 = number of whole 100-year periods within 400-year period
            var y100 = n / DaysPer100Years;
            // Last 100-year period has an extra day, so decrement result if 4
            if (y100 == 4) y100 = 3;
            // n = day number within 100-year period
            n -= y100 * DaysPer100Years;
            // y4 = number of whole 4-year periods within 100-year period
            var y4 = n / DaysPer4Years;
            // n = day number within 4-year period
            n -= y4 * DaysPer4Years;
            // y1 = number of whole years within 4-year period
            var y1 = n / DaysPerYear;
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4)
                y1 = 3;
            // If year was requested, compute and return it
            year = y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;
            // n = day number within year
            n -= y1 * DaysPerYear;
            // Leap year calculation looks different from IsLeapYear since y1, y4,
            // and y100 are relative to year 1, not year 0
            var leapYear = y1 == 3 && (y4 != 24 || y100 == 3);
            var days = leapYear? DaysToMonth366: DaysToMonth365;
            // All months have less than 32 days, so n >> 5 is a good conservative
            // estimate for the month
            var m = n >> 5 + 1;
            // m = 1-based month number
            while (n >= days[m]) m++;
            // If month was requested, return it
            month = m;
            // Return 1-based day-of-month
            day = n - days[m - 1] + 1;
        }
        #endif

    }

    [DisassemblyDiagnoser(printAsm: true, printSource: true)]
    //[HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]
    public class DateTimeStuff
    {
        DateTime _test;
        public DateTimeStuff()
        {
            _test = DateTime.Now;

            if (BaseLine() != Deconstructed())
                throw new Exception("bah");
        }

        [Benchmark(Baseline = true)]
        public int BaseLine() => _test.Year + _test.Month + _test.Day;


        [Benchmark]
        public int Deconstructed()
        {
            var (year, month, day) = _test;
            return year + month + day;
        }
    }
}
