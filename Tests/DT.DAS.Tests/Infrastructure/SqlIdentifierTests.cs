using DT.DAS.Infrastructure.Persistence;

namespace DT.DAS.Tests.Infrastructure;

public sealed class SqlIdentifierTests
{
    [Fact]
    public void SqlIdentifier_rejects_unsafe_table_names()
    {
        Assert.Equal("[dbo].[DA_AcquisitionConfig]", SqlIdentifier.Table("dbo.DA_AcquisitionConfig", "fallback"));
        Assert.Throws<ArgumentException>(() => SqlIdentifier.Table("DA_AcquisitionConfig;DROP TABLE X", "fallback"));
        Assert.Throws<ArgumentException>(() => SqlIdentifier.Table("DA_AcquisitionTask;DROP TABLE X", "fallback"));
        Assert.Throws<ArgumentException>(() => SqlIdentifier.Table("DA_AcquisitionTask_Group--", "fallback"));
    }
}
