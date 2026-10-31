using Microsoft.EntityFrameworkCore;

namespace Frontend_MarikinaAlert.Data
{
    /// <summary>
    /// EnsureCreated only builds brand-new databases, so columns and tables added later are patched in here.
    /// Each patch must be safe to run on every startup.
    /// </summary>
    public static class SchemaPatches
    {
        public static void Apply(AppDbContext db)
        {
            AddColumnIfMissing(db, "DisasterReports", "TrackingCode", "TEXT NULL");
            AddColumnIfMissing(db, "DisasterReports", "Source", "TEXT NOT NULL DEFAULT 'App'");
            AddColumnIfMissing(db, "DisasterReports", "LoggedBy", "TEXT NULL");
            db.Database.ExecuteSqlRaw(
                "CREATE INDEX IF NOT EXISTS \"IX_DisasterReports_TrackingCode\" ON \"DisasterReports\" (\"TrackingCode\");");

            // Agency directory and call log. Same shape EnsureCreated would make on a fresh database.
            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS "Agencies" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Agencies" PRIMARY KEY AUTOINCREMENT,
                    "Name" TEXT NOT NULL,
                    "Hotline" TEXT NULL,
                    "BackupNumber" TEXT NULL,
                    "RadioChannel" TEXT NULL,
                    "Notes" TEXT NULL,
                    "NumberSource" TEXT NULL,
                    "SortOrder" INTEGER NOT NULL,
                    "IsActive" INTEGER NOT NULL
                );
                """);
            db.Database.ExecuteSqlRaw(
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Agencies_Name\" ON \"Agencies\" (\"Name\");");
            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS "ContactAttempts" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_ContactAttempts" PRIMARY KEY AUTOINCREMENT,
                    "ReportId" INTEGER NOT NULL,
                    "AgencyName" TEXT NOT NULL,
                    "Number" TEXT NULL,
                    "Method" TEXT NOT NULL,
                    "Outcome" TEXT NOT NULL,
                    "LoggedBy" TEXT NULL,
                    "AtUtc" TEXT NOT NULL,
                    CONSTRAINT "FK_ContactAttempts_DisasterReports_ReportId" FOREIGN KEY ("ReportId") REFERENCES "DisasterReports" ("Id") ON DELETE CASCADE
                );
                """);
            db.Database.ExecuteSqlRaw(
                "CREATE INDEX IF NOT EXISTS \"IX_ContactAttempts_ReportId\" ON \"ContactAttempts\" (\"ReportId\");");

            // First run: list the agencies the triage suggests, with no numbers. Numbers must come from official sources.
            if (!db.Agencies.Any())
            {
                db.Agencies.AddRange(Models.Marikina.Agencies.Select((name, i) => new Models.Agency { Name = name, SortOrder = i }));
                db.SaveChanges();
            }
        }

        private static void AddColumnIfMissing(AppDbContext db, string table, string column, string definition)
        {
            var exists = db.Database
                .SqlQueryRaw<int>($"SELECT COUNT(*) AS \"Value\" FROM pragma_table_info('{table}') WHERE name = '{column}'")
                .AsEnumerable()
                .First() > 0;

            if (!exists)
                db.Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition};");
        }
    }
}
