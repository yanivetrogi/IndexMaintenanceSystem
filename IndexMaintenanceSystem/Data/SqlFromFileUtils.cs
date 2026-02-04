using System.Data;
using Dapper;

namespace IndexMaintenanceSystem.Data
{
    internal static class SqlFromFileUtils
    {
        internal static async Task ApplyStoredProcedureAsync(this IDbConnection imsConnection, string path)
        {
            var queryPieces = await SqlFromFileUtils.ReadSqlQueryAsync(path);
            foreach (var piece in queryPieces)
            {
                await imsConnection.ExecuteAsync(piece.ToString());
            }
        }

        internal static void ApplyStoredProcedure(this IDbConnection imsConnection, string path)
        {
            var queryPieces = SqlFromFileUtils.ReadSqlQueryAsync(path).Result;
            foreach (var piece in queryPieces)
            {
                imsConnection.Execute(piece.ToString());
            }
        }

        internal static string GetFullPath(string path) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

        internal static async Task<List<string>> ReadSqlQueryAsync(string path)
        {
            if (!System.IO.File.Exists(GetFullPath(path)))
            {
                throw new Exception($"{path} file is missing");
            }
            var query = await System.IO.File.ReadAllTextAsync(GetFullPath(path));
            return query.Replace("\r\n", "\n").Split("\nGO\n").Where(q => !string.IsNullOrWhiteSpace(q)).ToList();
        }

        internal static bool QueryExists(string path)
        {
            return File.Exists(SqlFromFileUtils.GetFullPath(path));
        }
    }
}
