using DevPulse.Domain.Entities;
using DevPulse.Domain.Enums;
using DevPulse.Shared.Contracts.Attendance;

namespace DevPulse.Application.Services.Attendance;

public sealed class AttendanceStatusCalculator
{
    public AttendanceDayStatusDto ComputeStatus(
        AttendanceRecord? record,
        AttendanceSettings settings,
        TimeZoneInfo timeZone)
    {
        if (record is null || (!record.PunchInUtc.HasValue && !record.PunchOutUtc.HasValue))
        {
            return AttendanceDayStatusDto.Absent;
        }

        if (!record.PunchInUtc.HasValue || !record.PunchOutUtc.HasValue)
        {
            return AttendanceDayStatusDto.Incomplete;
        }

        var punchInLocal = TimeZoneInfo.ConvertTimeFromUtc(record.PunchInUtc.Value, timeZone);
        var punchOutLocal = TimeZoneInfo.ConvertTimeFromUtc(record.PunchOutUtc.Value, timeZone);
        var punchInTime = TimeOnly.FromDateTime(punchInLocal);
        var punchOutTime = TimeOnly.FromDateTime(punchOutLocal);

        var isLate = punchInTime > settings.BufferStartTime;
        var isEarlyLeave = punchOutTime < settings.BufferEndTime;

        if (isLate && isEarlyLeave)
        {
            return AttendanceDayStatusDto.EarlyLeave;
        }

        if (isLate)
        {
            return AttendanceDayStatusDto.Late;
        }

        if (isEarlyLeave)
        {
            return AttendanceDayStatusDto.EarlyLeave;
        }

        return AttendanceDayStatusDto.OnTime;
    }

    public decimal? ComputeWorkHours(AttendanceRecord record)
    {
        if (!record.PunchInUtc.HasValue || !record.PunchOutUtc.HasValue)
        {
            return null;
        }

        var duration = record.PunchOutUtc.Value - record.PunchInUtc.Value;
        if (duration <= TimeSpan.Zero)
        {
            return null;
        }

        return Math.Round((decimal)duration.TotalHours, 2);
    }

    public AttendanceDayStatusDto MapStatus(AttendanceDayStatus status) => status switch
    {
        AttendanceDayStatus.OnTime => AttendanceDayStatusDto.OnTime,
        AttendanceDayStatus.Late => AttendanceDayStatusDto.Late,
        AttendanceDayStatus.EarlyLeave => AttendanceDayStatusDto.EarlyLeave,
        AttendanceDayStatus.Incomplete => AttendanceDayStatusDto.Incomplete,
        AttendanceDayStatus.Absent => AttendanceDayStatusDto.Absent,
        _ => AttendanceDayStatusDto.Absent
    };

    public static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    public DateOnly GetOfficeToday(TimeZoneInfo timeZone)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        return DateOnly.FromDateTime(localNow);
    }

    public bool CanPunchOutNow(AttendanceSettings settings, TimeZoneInfo timeZone, DateTime utcNow)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        return TimeOnly.FromDateTime(localNow) >= settings.BufferEndTime;
    }

    public static bool IsWeekend(DateOnly date, int weekendDaysBitmask)
    {
        var dayBit = 1 << (int)date.DayOfWeek;
        return (weekendDaysBitmask & dayBit) != 0;
    }
}
