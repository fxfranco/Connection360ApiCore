using Connection360.Domain.Entities;
using Connection360.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Domain.Interfaces
{
    public interface IDynamicDataSetMerger
    {
        DynamicDataSet Merge(IEnumerable<DynamicDataSet> dataSets, String joinField, DataSetJoinType joinType = DataSetJoinType.FullOuter);
    }
}
