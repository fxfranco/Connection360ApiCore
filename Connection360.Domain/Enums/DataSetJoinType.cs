using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Domain.Enum
{
    /// <summary>
    /// Estrategia de combinación (join) usada al mezclar varios conjuntos de datos por una llave
    /// en común.
    /// </summary>
    public enum DataSetJoinType
    {
        /// <summary>Conserva solo las llaves presentes en TODOS los datasets.</summary>
        Inner,

        /// <summary>Conserva todas las llaves de todos los datasets.</summary>
        FullOuter
    }
}
