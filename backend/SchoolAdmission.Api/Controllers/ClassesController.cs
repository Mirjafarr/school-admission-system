
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;

    public ClassesController(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public record CreateClassRequest(string Name, int TotalSeats);

    // ==========================================
    // HELPER: GET AUTHENTICATED STUDENT ID
    // ==========================================

    private string? GetAuthenticatedStudentId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
    }

    // ==========================================
    // HELPER: VALIDATE CLASS ID
    // ==========================================

    private static bool TryGetClassGuid(
        string classId,
        out Guid id)
    {
        id = Guid.Empty;

        return !string.IsNullOrWhiteSpace(classId)
            && classId.StartsWith("c_", StringComparison.Ordinal)
            && Guid.TryParse(classId.Substring(2), out id);
    }

    // ==========================================
    // 1. CREATE CLASS
    // POST /classes
    // ADMIN ONLY
    // ==========================================

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<IActionResult> CreateClass(
        [FromBody] CreateClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Trim().Length > 150 ||
            request.TotalSeats <= 0)
        {
            return BadRequest(new
            {
                error = "Class name must be 1-150 characters and totalSeats must be positive."
            });
        }

        const string sql = """
            INSERT INTO classes (name, total_seats)
            VALUES (@name, @totalSeats)
            RETURNING id;
            """;

        try
        {
            await using var connection =
                await _dataSource.OpenConnectionAsync();

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "name", request.Name.Trim());

            command.Parameters.AddWithValue(
                "totalSeats", request.TotalSeats);

            var id = (Guid)(await command.ExecuteScalarAsync())!;

            return StatusCode(201, new
            {
                id = $"c_{id}",
                name = request.Name.Trim(),
                totalSeats = request.TotalSeats
            });
        }
        catch (PostgresException ex)
            when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new
            {
                error = "A class with this name already exists."
            });
        }
    }

    // ==========================================
    // 2. GET ALL CLASSES
    // GET /classes
    // PUBLIC
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> GetClasses()
    {
        const string sql = """
            SELECT
                c.id,
                c.name,
                c.total_seats,
                COUNT(h.id) FILTER (
                    WHERE h.status = 'HELD'
                    AND h.expires_at > NOW()
                ) AS held_seats,
                COUNT(h.id) FILTER (
                    WHERE h.status = 'CONFIRMED'
                ) AS confirmed_seats
            FROM classes c
            LEFT JOIN admission_holds h
                ON h.class_id = c.id
            GROUP BY c.id, c.name, c.total_seats
            ORDER BY c.created_at;
            """;

        var classes = new List<object>();

        await using var connection =
            await _dataSource.OpenConnectionAsync();

        await using var command =
            new NpgsqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var id = reader.GetGuid(0);
            var name = reader.GetString(1);
            var totalSeats = reader.GetInt32(2);
            var heldSeats = reader.GetInt64(3);
            var confirmedSeats = reader.GetInt64(4);

            classes.Add(new
            {
                id = $"c_{id}",
                name,
                totalSeats,
                heldSeats,
                confirmedSeats,
                availableSeats =
                    totalSeats - heldSeats - confirmedSeats
            });
        }

        return Ok(classes);
    }

    // ==========================================
    // 3. GET SINGLE CLASS
    // GET /classes/{classId}
    // PUBLIC
    // ==========================================

    [HttpGet("{classId}")]
    public async Task<IActionResult> GetClass(string classId)
    {
        if (!TryGetClassGuid(classId, out var id))
        {
            return BadRequest(new
            {
                error = "Invalid class ID."
            });
        }

        const string sql = """
            SELECT
                c.id,
                c.name,
                c.total_seats,
                COUNT(h.id) FILTER (
                    WHERE h.status = 'HELD'
                    AND h.expires_at > NOW()
                ) AS held_seats,
                COUNT(h.id) FILTER (
                    WHERE h.status = 'CONFIRMED'
                ) AS confirmed_seats
            FROM classes c
            LEFT JOIN admission_holds h
                ON h.class_id = c.id
            WHERE c.id = @id
            GROUP BY c.id, c.name, c.total_seats;
            """;

        await using var connection =
            await _dataSource.OpenConnectionAsync();

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", id);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return NotFound(new
            {
                error = "Class not found."
            });
        }

        var totalSeats = reader.GetInt32(2);
        var heldSeats = reader.GetInt64(3);
        var confirmedSeats = reader.GetInt64(4);

        return Ok(new
        {
            id = $"c_{reader.GetGuid(0)}",
            name = reader.GetString(1),
            totalSeats,
            heldSeats,
            confirmedSeats,
            availableSeats =
                totalSeats - heldSeats - confirmedSeats
        });
    }

    // ==========================================
    // 4. CREATE SEAT RESERVATION
    // POST /classes/{classId}/holds
    // STUDENT ONLY
    // ==========================================

    [Authorize(Policy = "StudentOnly")]
    [HttpPost("{classId}/holds")]
    public async Task<IActionResult> CreateHold(string classId)
    {
        var studentId = GetAuthenticatedStudentId();

        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length > 17)
        {
            return Unauthorized(new
            {
                error = "Invalid student identity in token."
            });
        }

        if (!TryGetClassGuid(classId, out var id))
        {
            return BadRequest(new
            {
                error = "Invalid class ID."
            });
        }

        try
        {
            await using var connection =
                await _dataSource.OpenConnectionAsync();

            await using var transaction =
                await connection.BeginTransactionAsync();

            // Lock class row to prevent overbooking.

            const string classSql = """
                SELECT total_seats
                FROM classes
                WHERE id = @id
                FOR UPDATE;
                """;

            int totalSeats;

            await using (var command =
                new NpgsqlCommand(classSql, connection, transaction))
            {
                command.Parameters.AddWithValue("id", id);

                var result = await command.ExecuteScalarAsync();

                if (result is null)
                {
                    await transaction.RollbackAsync();

                    return NotFound(new
                    {
                        error = "Class not found."
                    });
                }

                totalSeats = Convert.ToInt32(result);
            }

            // Expire old reservations.

            const string expireSql = """
                UPDATE admission_holds
                SET status = 'EXPIRED'
                WHERE class_id = @id
                  AND status = 'HELD'
                  AND expires_at <= NOW();
                """;

            await using (var command =
                new NpgsqlCommand(expireSql, connection, transaction))
            {
                command.Parameters.AddWithValue("id", id);

                await command.ExecuteNonQueryAsync();
            }

            // Check existing active reservation.

            const string existingSql = """
                SELECT EXISTS (
                    SELECT 1
                    FROM admission_holds
                    WHERE class_id = @id
                      AND student_id = @studentId
                      AND (
                          status = 'CONFIRMED'
                          OR (
                              status = 'HELD'
                              AND expires_at > NOW()
                          )
                      )
                );
                """;

            await using (var command =
                new NpgsqlCommand(existingSql, connection, transaction))
            {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("studentId", studentId);

                var exists =
                    (bool)(await command.ExecuteScalarAsync())!;

                if (exists)
                {
                    await transaction.RollbackAsync();

                    return Conflict(new
                    {
                        error = "Student already has an active reservation for this class."
                    });
                }
            }

            // Count occupied seats.

            const string countSql = """
                SELECT COUNT(*)
                FROM admission_holds
                WHERE class_id = @id
                  AND (
                      status = 'CONFIRMED'
                      OR (
                          status = 'HELD'
                          AND expires_at > NOW()
                      )
                  );
                """;

            long occupiedSeats;

            await using (var command =
                new NpgsqlCommand(countSql, connection, transaction))
            {
                command.Parameters.AddWithValue("id", id);

                occupiedSeats = Convert.ToInt64(
                    await command.ExecuteScalarAsync());
            }

            if (occupiedSeats >= totalSeats)
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    error = "No available seats in this class."
                });
            }

            // Create a 10-minute reservation.

            const string insertSql = """
                INSERT INTO admission_holds
                    (class_id, student_id, status, expires_at)
                VALUES
                    (@id, @studentId, 'HELD',
                     NOW() + INTERVAL '600 seconds')
                RETURNING id, expires_at;
                """;

            Guid holdId;
            DateTime expiresAt;

            await using (var command =
                new NpgsqlCommand(insertSql, connection, transaction))
            {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("studentId", studentId);

                await using var reader =
                    await command.ExecuteReaderAsync();

                await reader.ReadAsync();

                holdId = reader.GetGuid(0);
                expiresAt = reader.GetDateTime(1);
            }

            await transaction.CommitAsync();

            return StatusCode(201, new
            {
                id = holdId,
                classId,
                studentId,
                status = "HELD",
                expiresAt
            });
        }
        catch (PostgresException ex)
            when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new
            {
                error = "Student already has an active reservation for this class."
            });
        }
    }

    // ==========================================
    // 5. CONFIRM RESERVATION
    // POST /classes/{classId}/holds/{holdId}/confirm
    // STUDENT ONLY + OWNER ONLY
    // ==========================================

    [Authorize(Policy = "StudentOnly")]
    [HttpPost("{classId}/holds/{holdId:guid}/confirm")]
    public async Task<IActionResult> ConfirmHold(
        string classId,
        Guid holdId)
    {
        var studentId = GetAuthenticatedStudentId();

        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length > 17)
        {
            return Unauthorized(new
            {
                error = "Invalid student identity in token."
            });
        }

        if (!TryGetClassGuid(classId, out var id))
        {
            return BadRequest(new
            {
                error = "Invalid class ID."
            });
        }

        await using var connection =
            await _dataSource.OpenConnectionAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        // Expire only this student's expired reservation.

        const string expireSql = """
            UPDATE admission_holds
            SET status = 'EXPIRED'
            WHERE id = @holdId
              AND class_id = @classId
              AND student_id = @studentId
              AND status = 'HELD'
              AND expires_at <= NOW();
            """;

        await using (var command =
            new NpgsqlCommand(expireSql, connection, transaction))
        {
            command.Parameters.AddWithValue("holdId", holdId);
            command.Parameters.AddWithValue("classId", id);
            command.Parameters.AddWithValue("studentId", studentId);

            await command.ExecuteNonQueryAsync();
        }

        // Confirm only the authenticated student's reservation.

        const string confirmSql = """
            UPDATE admission_holds
            SET status = 'CONFIRMED',
                confirmed_at = NOW()
            WHERE id = @holdId
              AND class_id = @classId
              AND student_id = @studentId
              AND status = 'HELD'
              AND expires_at > NOW()
            RETURNING student_id, confirmed_at;
            """;

        DateTime confirmedAt;

        await using (var command =
            new NpgsqlCommand(confirmSql, connection, transaction))
        {
            command.Parameters.AddWithValue("holdId", holdId);
            command.Parameters.AddWithValue("classId", id);
            command.Parameters.AddWithValue("studentId", studentId);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                await reader.CloseAsync();

                await transaction.CommitAsync();

                return Conflict(new
                {
                    error = "Reservation not found, expired, already processed, or does not belong to you."
                });
            }

            confirmedAt = reader.GetDateTime(1);
        }

        await transaction.CommitAsync();

        return Ok(new
        {
            id = holdId,
            classId,
            studentId,
            status = "CONFIRMED",
            confirmedAt
        });
    }

    // ==========================================
    // 6. CANCEL RESERVATION
    // DELETE /holds/{holdId}
    // STUDENT ONLY + OWNER ONLY
    // ==========================================

    [Authorize(Policy = "StudentOnly")]
    [HttpDelete("/holds/{holdId:guid}")]
    public async Task<IActionResult> CancelHold(Guid holdId)
    {
        var studentId = GetAuthenticatedStudentId();

        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length > 17)
        {
            return Unauthorized(new
            {
                error = "Invalid student identity in token."
            });
        }

        await using var connection =
            await _dataSource.OpenConnectionAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        const string findSql = """
            SELECT
                status,
                expires_at > NOW() AS is_active
            FROM admission_holds
            WHERE id = @holdId
              AND student_id = @studentId
            FOR UPDATE;
            """;

        string? status = null;
        bool isActive = false;
        bool found = false;

        await using (var command =
            new NpgsqlCommand(findSql, connection, transaction))
        {
            command.Parameters.AddWithValue("holdId", holdId);
            command.Parameters.AddWithValue("studentId", studentId);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                found = true;
                status = reader.GetString(0);
                isActive = reader.GetBoolean(1);
            }
        }

        if (!found)
        {
            await transaction.RollbackAsync();

            return NotFound(new
            {
                error = "Reservation not found."
            });
        }

        // Handle expired reservation.

        if (status == "HELD" && !isActive)
        {
            const string expireSql = """
                UPDATE admission_holds
                SET status = 'EXPIRED'
                WHERE id = @holdId
                  AND student_id = @studentId;
                """;

            await using var command =
                new NpgsqlCommand(expireSql, connection, transaction);

            command.Parameters.AddWithValue("holdId", holdId);
            command.Parameters.AddWithValue("studentId", studentId);

            await command.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            return Conflict(new
            {
                error = "Reservation has expired."
            });
        }

        // Only HELD reservations can be cancelled.

        if (status != "HELD")
        {
            await transaction.CommitAsync();

            return Conflict(new
            {
                error = "Only active HELD reservations can be cancelled."
            });
        }

        const string cancelSql = """
            UPDATE admission_holds
            SET status = 'CANCELLED'
            WHERE id = @holdId
              AND student_id = @studentId
              AND status = 'HELD'
              AND expires_at > NOW();
            """;

        await using (var command =
            new NpgsqlCommand(cancelSql, connection, transaction))
        {
            command.Parameters.AddWithValue("holdId", holdId);
            command.Parameters.AddWithValue("studentId", studentId);

            int affectedRows = await command.ExecuteNonQueryAsync();

            if (affectedRows != 1)
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    error = "Reservation could not be cancelled."
                });
            }
        }

        await transaction.CommitAsync();

        return NoContent();
    }

    // ==========================================
    // 7. GET MY RESERVATIONS
    // GET /me/holds
    // STUDENT ONLY
    // ==========================================

    [Authorize(Policy = "StudentOnly")]
    [HttpGet("/me/holds")]
    public async Task<IActionResult> GetMyHolds()
    {
        var studentId = GetAuthenticatedStudentId();

        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length > 17)
        {
            return Unauthorized(new
            {
                error = "Invalid student identity in token."
            });
        }

        const string sql = """
            SELECT
                h.id,
                h.class_id,
                c.name,
                h.student_id,
                h.status,
                h.created_at,
                h.expires_at,
                h.confirmed_at
            FROM admission_holds h
            INNER JOIN classes c
                ON c.id = h.class_id
            WHERE h.student_id = @studentId
            ORDER BY h.created_at DESC;
            """;

        var holds = new List<object>();

        await using var connection =
            await _dataSource.OpenConnectionAsync();

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("studentId", studentId);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            holds.Add(new
            {
                id = reader.GetGuid(0),
                classId = $"c_{reader.GetGuid(1)}",
                className = reader.GetString(2),
                studentId = reader.GetString(3),
                status = reader.GetString(4),
                createdAt = reader.GetDateTime(5),
                expiresAt = reader.GetDateTime(6),
                confirmedAt = reader.IsDBNull(7)
                    ? (DateTime?)null
                    : reader.GetDateTime(7)
            });
        }

        return Ok(holds);
    }

    // ==========================================
    // 8. GET SINGLE RESERVATION
    // GET /holds/{holdId}
    // STUDENT ONLY + OWNER ONLY
    // ==========================================

    [Authorize(Policy = "StudentOnly")]
    [HttpGet("/holds/{holdId:guid}")]
    public async Task<IActionResult> GetHold(Guid holdId)
    {
        var studentId = GetAuthenticatedStudentId();

        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length > 17)
        {
            return Unauthorized(new
            {
                error = "Invalid student identity in token."
            });
        }

        await using var connection =
            await _dataSource.OpenConnectionAsync();

        // Update this student's expired reservation.

        const string expireSql = """
            UPDATE admission_holds
            SET status = 'EXPIRED'
            WHERE id = @holdId
              AND student_id = @studentId
              AND status = 'HELD'
              AND expires_at <= NOW();
            """;

        await using (var command =
            new NpgsqlCommand(expireSql, connection))
        {
            command.Parameters.AddWithValue("holdId", holdId);
            command.Parameters.AddWithValue("studentId", studentId);

            await command.ExecuteNonQueryAsync();
        }

        // Retrieve only the authenticated student's reservation.

        const string sql = """
            SELECT
                h.id,
                h.class_id,
                c.name,
                h.status,
                h.created_at,
                h.expires_at,
                h.confirmed_at
            FROM admission_holds h
            INNER JOIN classes c
                ON c.id = h.class_id
            WHERE h.id = @holdId
              AND h.student_id = @studentId;
            """;

        await using var command2 =
            new NpgsqlCommand(sql, connection);

        command2.Parameters.AddWithValue("holdId", holdId);
        command2.Parameters.AddWithValue("studentId", studentId);

        await using var reader =
            await command2.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return NotFound(new
            {
                error = "Reservation not found."
            });
        }

        return Ok(new
        {
            holdId = reader.GetGuid(0),
            classId = $"c_{reader.GetGuid(1)}",
            className = reader.GetString(2),
            status = reader.GetString(3),
            createdAt = reader.GetDateTime(4),
            expiresAt = reader.GetDateTime(5),
            confirmedAt = reader.IsDBNull(6)
                ? (DateTime?)null
                : reader.GetDateTime(6)
        });
    }

    
    // ==========================================
    // 9. ADMIN: GET ALL ADMISSIONS
    // GET /admin/admissions
    // ADMIN ONLY
    // ==========================================

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("/admin/admissions")]
    public async Task<IActionResult> GetAllAdmissions()
    {
        const string sql = """
            SELECT
                h.id,
                h.class_id,
                c.name,
                h.student_id,
                CASE
                    WHEN h.status = 'HELD'
                         AND h.expires_at <= NOW()
                    THEN 'EXPIRED'
                    ELSE h.status
                END AS effective_status,
                h.created_at,
                h.expires_at,
                h.confirmed_at
            FROM admission_holds h
            INNER JOIN classes c
                ON c.id = h.class_id
            ORDER BY h.created_at DESC;
            """;

        var admissions = new List<object>();

        await using var connection =
            await _dataSource.OpenConnectionAsync();

        await using var command =
            new NpgsqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            admissions.Add(new
            {
                id = reader.GetGuid(0),
                classId = $"c_{reader.GetGuid(1)}",
                className = reader.GetString(2),
                studentId = reader.GetString(3),
                status = reader.GetString(4),
                createdAt = reader.GetDateTime(5),
                expiresAt = reader.GetDateTime(6),
                confirmedAt = reader.IsDBNull(7)
                    ? (DateTime?)null
                    : reader.GetDateTime(7)
            });
        }

        return Ok(admissions);
    }

   
}
