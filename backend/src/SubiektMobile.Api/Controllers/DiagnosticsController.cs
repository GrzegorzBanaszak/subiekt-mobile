using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SubiektMobile.Infrastructure.Persistence;

namespace SubiektMobile.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly SubiektDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public DiagnosticsController(SubiektDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpGet("db-debug")]
    public async Task<IActionResult> CheckDatabaseDebug(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("SubiektGt");

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT DB_NAME()";

            var dbName = await command.ExecuteScalarAsync(cancellationToken);

            return Ok(new
            {
                status = "ok",
                message = "Połączenie działa.",
                database = dbName
            });
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new
            {
                status = "sql_error",
                message = ex.Message,
                number = ex.Number,
                server = ex.Server,
                procedure = ex.Procedure,
                lineNumber = ex.LineNumber
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                status = "error",
                message = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }
}