using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using System.Collections.Generic;

namespace Connection360.Etl.Domain.Interfaces
{
    /// <summary>
    /// Puerto de dominio para combinar varios <see cref="DynamicDataSet"/> (uno por API externa) en
    /// uno solo, por un campo llave común. Copiado de Connection360.Domain.Interfaces.IDynamicDataSetMerger.
    /// </summary>
    public interface IDynamicDataSetMerger
    {
        DynamicDataSet Merge(IEnumerable<DynamicDataSet> dataSets, String joinField, DataSetJoinType joinType = DataSetJoinType.FullOuter);
    }
}
