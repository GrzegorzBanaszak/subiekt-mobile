using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SubiektMobile.Infrastructure.Persistence;
using SubiektMobile.Application.Identity;

namespace SubiektMobile.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.IdentityManage)]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly SubiektDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public DiagnosticsController(
        SubiektDbContext dbContext,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet("db-debug")]
    public async Task<IActionResult> CheckDatabaseDebug(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

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
