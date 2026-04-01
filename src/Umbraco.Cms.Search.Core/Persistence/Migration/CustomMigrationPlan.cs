using Umbraco.Cms.Core.Packaging;

namespace Umbraco.Cms.Search.Core.Persistence.Migration;

public class CustomPackageMigrationPlan : PackageMigrationPlan
{
    public CustomPackageMigrationPlan() : base("Umbraco CMS Search")
    {
    }

    protected override void DefinePlan()
    {
        To<CustomPackageMigration>(new Guid("4FD681BE-E27E-4688-922B-29EDCDCB8A49"));
        To<AddIndexDocumentFieldsTableMigration>(new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"));
    }
}
