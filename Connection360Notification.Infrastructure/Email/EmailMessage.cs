namespace Connection360Notification.Infrastructure.Email
{
    /// <summary>
    /// Correo ya armado, independiente de la librería SMTP: destinatario, asunto y cuerpo. <paramref name="Body"/> es el
    /// texto plano; si se informa <paramref name="HtmlBody"/> el correo se envía como "multipart/alternative" (texto +
    /// HTML) y, si además hay <paramref name="InlineImage"/>, la imagen viaja incrustada para referenciarla en el HTML
    /// con <c>cid:</c> + su ContentId.
    /// </summary>
    public sealed record EmailMessage(String To, String Subject, String Body, String? HtmlBody = null, EmailInlineImage? InlineImage = null);

    /// <summary>Imagen incrustada en el cuerpo HTML (se referencia como <c>&lt;img src="cid:ContentId"&gt;</c>).</summary>
    public sealed record EmailInlineImage(String ContentId, Byte[] Content, String MediaType);
}
