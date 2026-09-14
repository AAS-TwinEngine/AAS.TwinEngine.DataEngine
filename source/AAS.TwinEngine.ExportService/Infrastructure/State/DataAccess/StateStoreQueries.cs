namespace AAS.TwinEngine.ExportService.Infrastructure.State.DataAccess;

/// <summary>
/// SQL statements used by the state store. Kept in code (no external .sql files) so the
/// schema and queries live next to the code that uses them.
/// The <c>{0}</c> placeholder in each statement is replaced with the configured schema name.
/// </summary>
internal static class StateStoreQueries
{
    public const string CreateSchema = @"
CREATE SCHEMA IF NOT EXISTS {0};

CREATE TABLE IF NOT EXISTS {0}.exported_entities (
    entity_kind      VARCHAR(50)   NOT NULL,
    identifier       TEXT          NOT NULL,
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    last_synced_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_exported_entities PRIMARY KEY (entity_kind, identifier)
);

ALTER TABLE {0}.exported_entities DROP COLUMN IF EXISTS content_hash;

CREATE INDEX IF NOT EXISTS idx_exported_entities_kind
    ON {0}.exported_entities (entity_kind);
";

    public const string SelectByKind = @"
SELECT entity_kind, identifier, created_at, last_synced_at
FROM {0}.exported_entities
WHERE entity_kind = @entity_kind;
";

    public const string Upsert = @"
INSERT INTO {0}.exported_entities (entity_kind, identifier, created_at, last_synced_at)
VALUES (@entity_kind, @identifier, @created_at, @last_synced_at)
ON CONFLICT (entity_kind, identifier) DO UPDATE SET
    last_synced_at = EXCLUDED.last_synced_at;
";

    public const string Delete = @"
DELETE FROM {0}.exported_entities
WHERE entity_kind = @entity_kind AND identifier = @identifier;
";
}
