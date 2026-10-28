namespace Connection360.Etl.Domain.Enums
{
    /// <summary>
    /// Estrategia de unión al combinar varios <see cref="Connection360.Etl.Domain.Entities.DynamicDataSet"/>
    /// por un campo llave (ver <see cref="Connection360.Etl.Domain.Services.DynamicDataSetMerger"/>).
    /// Copiado de Connection360.Domain.Enum.DataSetJoinType.
    /// </summary>
    public enum DataSetJoinType
    {
        /// <summary>Conserva únicamente las llaves presentes en TODOS los conjuntos de datos.</summary>
        Inner,

        /// <summary>Conserva las llaves presentes en CUALQUIERA de los conjuntos de datos.</summary>
        FullOuter
    }
}
