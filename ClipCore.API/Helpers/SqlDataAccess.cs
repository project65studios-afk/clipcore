using System.Data;
using ClipCore.API.Interfaces;
using Dapper;
using Npgsql;

namespace ClipCore.API.Helpers;

public class SqlDataAccess : ISqlDataAccess
{
    private readonly IConfiguration _config;

    public SqlDataAccess(IConfiguration config) => _config = config;

    private IDbConnection CreateConnection() =>
        new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));

    public async Task<IEnumerable<T>> LoadData<T, U>(string sql, U parameters)
    {
        using IDbConnection conn = CreateConnection();
        return await conn.QueryAsync<T>(sql, parameters);
    }

    public async Task<IEnumerable<T>> LoadData<T>(string sql)
    {
        using IDbConnection conn = CreateConnection();
        return await conn.QueryAsync<T>(sql);
    }

    public async Task<T?> LoadSingle<T, U>(string sql, U parameters)
    {
        using IDbConnection conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    public async Task SaveData<T>(string sql, T parameters)
    {
        using IDbConnection conn = CreateConnection();
        await conn.ExecuteAsync(sql, parameters);
    }

    public async Task<T?> ExecuteScalar<T, U>(string sql, U parameters)
    {
        using IDbConnection conn = CreateConnection();
        return await conn.ExecuteScalarAsync<T>(sql, parameters);
    }
}
