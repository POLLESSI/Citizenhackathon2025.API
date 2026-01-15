using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Init
{
    public static class DbInit
    {
        public static async Task RunOnceAsync(IDbConnection conn, string contentRoot, ILogger log)
        {
            // 2.1 – Table de garde __AppOnce
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                            IF OBJECT_ID('dbo.__AppOnce', 'U') IS NULL
                            BEGIN
                              CREATE TABLE dbo.__AppOnce (Name sysname PRIMARY KEY, RanAtUtc datetime2 NOT NULL);
                            END;";
                cmd.ExecuteNonQuery();
            }

            // 2.2 – Si PostDeploy_GPT pas encore exécuté => exécuter tes scripts .sql
            if (!HasRun(conn, "PostDeploy_GPT"))
            {
                // Exécute d'abord le hotfix des index GPT (pour virer les doublons)
                ExecuteSqlBatches(conn, ReadSql(contentRoot, "sql/01_fix_gpt_indexes.sql"));

                // Puis ton gros Post-Deployment idempotent
                ExecuteSqlBatches(conn, ReadSql(contentRoot, "sql/99_post_deploy.sql"));

                MarkRan(conn, "PostDeploy_GPT");
                log.LogInformation("PostDeploy_GPT executed.");
            }
            else
            {
                log.LogInformation("PostDeploy_GPT already executed. Skipping.");
            }
        }

        private static bool HasRun(IDbConnection conn, string name)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM dbo.__AppOnce WHERE Name = @n";
            var p = cmd.CreateParameter(); p.ParameterName = "@n"; p.Value = name; cmd.Parameters.Add(p);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void MarkRan(IDbConnection conn, string name)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO dbo.__AppOnce(Name, RanAtUtc) VALUES (@n, SYSUTCDATETIME())";
            var p = cmd.CreateParameter(); p.ParameterName = "@n"; p.Value = name; cmd.Parameters.Add(p);
            cmd.ExecuteNonQuery();
        }

        private static string ReadSql(string root, string relativePath)
            => File.ReadAllText(Path.Combine(root, relativePath));

        // SqlCommand ne comprend pas "GO" => il faut découper en batches
        private static void ExecuteSqlBatches(IDbConnection conn, string sql)
        {
            var batches = Regex.Split(sql, @"^\s*GO\s*$(?mi)");
            foreach (var batch in batches.Select(b => b.Trim()).Where(b => b.Length > 0))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = batch;
                cmd.CommandTimeout = 0;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.