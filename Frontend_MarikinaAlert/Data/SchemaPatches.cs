using Microsoft.EntityFrameworkCore;

namespace Frontend_MarikinaAlert.Data
{
    /// <summary>
    /// EnsureCreated only builds brand-new databases, so columns added later are patched in here.
    /// Each patch must be safe to run on every startup.
    /// </summary>
    public static class SchemaPatches
    {
        public static void Apply(AppDbContext db)
        {
            AddColumnIfMissing(db, "DisasterReports", "TrackingCode", "TEXT NULL");
            db.Database.ExecuteSqlRaw(
                "CREATE INDEX IF NOT EXISTS \"IX_DisasterReports_TrackingCode\" ON \"DisasterReports\" (\"TrackingCode\");");
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
