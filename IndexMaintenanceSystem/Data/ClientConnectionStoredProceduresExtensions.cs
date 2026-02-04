using System.Data;
using Dapper;

namespace IndexMaintenanceSystem.Data;

public static class ClientConnectionStoredProceduresExtension
{
    private static readonly string IndexDefragInfoQueryPath = "Scripts/sp_index_defrag_info.sql";
    private static readonly string DropIndexDefragInfoSql = 
@"
IF OBJECT_ID('dbo.sp_index_defrag_info') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[sp_index_defrag_info];
END
";

    public static void ValidateStoredProcedureDefinitionFilesExist()
    {
        if (!SqlFromFileUtils.QueryExists(IndexDefragInfoQueryPath))
        {
            throw new Exception($"{IndexDefragInfoQueryPath} stored procedure definition file does not exist");
        }
    }

    public static void ApplyIndexDefragInfoStoredProcedure(this IDbConnection clientConnection) =>
        clientConnection.ApplyStoredProcedure(IndexDefragInfoQueryPath);

    public static void DropIndexDefragInfoStoredProcedure(this IDbConnection connection) =>
        connection.Execute(DropIndexDefragInfoSql);
}
