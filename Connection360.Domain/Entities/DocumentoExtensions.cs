namespace Connection360.Domain.Entities
{
    public static class DocumentoExtensions
    {
        public static String GetDocumentoSinPrefijo(this String documento)
        {
            if (String.IsNullOrWhiteSpace(documento))
                return String.Empty;

            var partes = documento.Split('-');
            return partes.Length > 1 ? partes[1].Trim() : documento;
        }
    }
}
