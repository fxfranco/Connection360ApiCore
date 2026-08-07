using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Domain.Enum
{
    public enum DataSetJoinType
    {
        /// <summary>Conserva solo las llaves presentes en TODOS los datasets.</summary>
        Inner,

        /// <summary>Conserva todas las llaves de todos los datasets.</summary>
        FullOuter
    }
}
