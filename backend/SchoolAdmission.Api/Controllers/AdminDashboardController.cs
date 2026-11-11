
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("admin/dashboard")]
[Authorize(Policy = "AdminOnly")]
public class AdminDashboardController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;

    public AdminDashboardController(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // ==========================================
    // 1. GET ADMIN DASHBOARD SUMMARY
    // GET /admin/dashboard
    // ADMIN ONLY
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM classes)
                    AS total_classes,

                COALESCE(
                    (SELECT SUM(total_seats) FROM classes),
                    0
                ) AS total_seats,

                (
                    SELECT COUNT(*)
                    FROM students
                ) AS total_students,

                (
                    SELECT COUNT(*)
                    FROM admission_holds
                    WHERE status = 'HELD'
                    AND expires_at > NOW()
                ) AS held_seats,

                (
                    SELECT COUNT(*)
                    FROM admission_holds
                    WHERE status = 'CONFIRMED'
                ) AS confirmed_seats;
            """;

        await using var connection =
            await _dataSource.OpenConnectionAsync();

        await using var command =
            new NpgsqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return StatusCode(500, new
            {
                error = "Unable to retrieve dashboard statistics."
            });
        }

        var totalClasses = reader.GetInt64(0);
        var totalSeats = reader.GetInt64(1);
        var totalStudents = reader.GetInt64(2);
        var heldSeats = reader.GetInt64(3);
        var confirmedSeats = reader.GetInt64(4);

        var availableSeats =
            totalSeats - heldSeats - confirmedSeats;

        return Ok(new
        {
            summary = new
            {
                totalClasses,
                totalStudents,
                totalSeats,
                heldSeats,
                confirmedSeats,
                availableSeats
            }
        });
    }
}
