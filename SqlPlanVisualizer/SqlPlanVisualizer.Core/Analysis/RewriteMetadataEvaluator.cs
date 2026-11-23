using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Core.Analysis;

public static class RewriteMetadataEvaluator
{
    public static RewriteMetadataCheck Evaluate(RewriteTypeRequirement requirement, DatabaseColumnMetadata? column)
    {
        if (column is null) return RewriteMetadataCheck.NotChecked("Az oszlop nem található, vagy nincs hozzá olvasási jogosultság.");
        if (column.IsDomain || column.TypeSchema != "pg_catalog")
            return new(MetadataCheckStatus.Unknown, "Domain vagy egyéni típus: az alkalmazhatóság külön ellenőrzést igényel.", column);
        bool supported = requirement switch
        {
            RewriteTypeRequirement.DateOrTimestamp => column.TypeName is "date" or "timestamp",
            RewriteTypeRequirement.Integer => column.TypeName is "int2" or "int4" or "int8",
            RewriteTypeRequirement.Text => column.TypeName is "text" or "varchar",
            _ => false
        };
        return new(supported ? MetadataCheckStatus.Passed : MetadataCheckStatus.Failed,
            supported ? "A típusfeltétel teljesül. A túlcsordulás, kolláció és egyéb előfeltételek nincsenek igazolva." :
            "A típusfeltétel nem teljesül. Ezt az alternatívát nem futtatjuk; válassz másik javaslatot vagy ellenőrizd újra a metaadatokat.", column);
    }
}
