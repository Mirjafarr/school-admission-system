
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("admin/students")]
[Authorize(Policy = "AdminOnly")]
public class StudentsController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;

    public StudentsController(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // GET /admin/students
    // Admin only

    [HttpGet]
    public async Task<IActionResult> GetStudents()
    {
        const string sql = """
            SELECT
                s.id,
                s.student_id,
                s.created_at,

                COUNT(h.id)::int AS application_count,

                (COUNT(h.id) FILTER (
                    WHERE h.status = 'CONFIRMED'
                ))::int AS confirmed_admissions,

                MAX(h.created_at) AS last_application_at,

                (
                    SELECT
                        CASE
                            WHEN h2.status = 'HELD'
                                 AND h2.expires_at <= NOW()
                            THEN 'EXPIRED'
                            ELSE h2.status
                        END
                    FROM admission_holds h2
                    WHERE h2.student_id = s.student_id
                    ORDER BY h2.created_at DESC
                    LIMIT 1
                ) AS latest_admission_status

            FROM students s

            LEFT JOIN admission_holds h
                ON h.student_id = s.student_id

            WHERE s.role = 'Student'

            GROUP BY
                s.id,
                s.student_id,
                s.created_at

            ORDER BY s.created_at DESC;
            """;

        var students = new List<object>();

        await using var connection =
            await _dataSource.OpenConnectionAsync();

        await using var command =
            new NpgsqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            students.Add(new
            {
                id = reader.GetGuid(0),
                studentId = reader.GetString(1),
                createdAt = reader.GetDateTime(2),
                applicationCount = reader.GetInt32(3),
                confirmedAdmissions = reader.GetInt32(4),

                lastApplicationAt = reader.IsDBNull(5)
                    ? (DateTime?)null
                    : reader.GetDateTime(5),

                latestAdmissionStatus = reader.IsDBNull(6)
                    ? null
                    : reader.GetString(6)
            });
        }

        return Ok(students);
    }
}
