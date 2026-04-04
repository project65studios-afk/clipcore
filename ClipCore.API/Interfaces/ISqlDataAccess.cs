namespace ClipCore.API.Interfaces;

public interface ISqlDataAccess
{
    Task<IEnumerable<T>> LoadData<T, U>(string sql, U parameters);
    Task<IEnumerable<T>> LoadData<T>(string sql);
    Task<T?> LoadSingle<T, U>(string sql, U parameters);
    Task SaveData<T>(string sql, T parameters);
    Task<T?> ExecuteScalar<T, U>(string sql, U parameters);
}
