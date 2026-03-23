using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Init
{
    public static class DbInit
    {

        public static async Task RunOnceAsync(IDbConnection conn, string contentRoot, ILogger log)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                                IF OBJECT_ID('dbo.__AppOnce', 'U') IS NULL
                                BEGIN
                                    CREATE TABLE dbo.__AppOnce
                                    (
                                        Name sysname PRIMARY KEY,
                                        RanAtUtc datetime2 NOT NULL
                                    );
                                END;";
                cmd.ExecuteNonQuery();
            }

            if (!HasRun(conn, "PostDeploy_GPT"))
            {
                var fixPath = ResolveSqlPath(contentRoot, "sql/01_fix_gpt_indexes.sql");
                var postDeployPath = ResolveSqlPath(contentRoot, "sql/99_post_deploy.sql");

                log.LogInformation("Executing SQL init file: {File}", fixPath);
                ExecuteSqlBatches(conn, await File.ReadAllTextAsync(fixPath));

                log.LogInformation("Executing SQL init file: {File}", postDeployPath);
                ExecuteSqlBatches(conn, await File.ReadAllTextAsync(postDeployPath));

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
            var p = cmd.CreateParameter();
            p.ParameterName = "@n";
            p.Value = name;
            cmd.Parameters.Add(p);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void MarkRan(IDbConnection conn, string name)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO dbo.__AppOnce(Name, RanAtUtc) VALUES (@n, SYSUTCDATETIME())";
            var p = cmd.CreateParameter();
            p.ParameterName = "@n";
            p.Value = name;
            cmd.Parameters.Add(p);

            cmd.ExecuteNonQuery();
        }

        private static string ResolveSqlPath(string root, string relativePath)
        {
            var fullPath = Path.Combine(root, relativePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    $"SQL init file not found. Root='{root}', Relative='{relativePath}', FullPath='{fullPath}'",
                    fullPath);
            }

            return fullPath;
        }

        private static void ExecuteSqlBatches(IDbConnection conn, string sql)
        {
            var batches = Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (var batch in batches.Select(b => b.Trim()).Where(b => !string.IsNullOrWhiteSpace(b)))
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