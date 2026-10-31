namespace Connection360Notification.Domain
{
    /// <summary>
    /// Datos básicos de un usuario (id de Auth0, documento, nombre completo, teléfono, correo y rol) que Connection360.Api
    /// sincroniza hacia este servicio para poder notificar a la persona sin consultar Auth0 en cada notificación.
    /// Se guarda en la colección <c>basicUserData</c>; document, fullName y phone salen del <c>user_metadata</c> del usuario.
    /// </summary>
    /// <param name="Id">Identificador único del usuario en Auth0 (<c>user_id</c>); es la llave: no puede repetirse.</param>
    /// <param name="Document">Documento de identificación (NIT/cédula) del usuario, el mismo que se usa como <c>ClientId</c> de las notificaciones.</param>
    /// <param name="FullName">Nombre completo del usuario.</param>
    /// <param name="Phone">Teléfono del usuario (<c>user_metadata.phone.number</c>).</param>
    /// <param name="Email">Correo electrónico del usuario (atributo <c>email</c> de Auth0).</param>
    /// <param name="RoleName">Nombre del rol asignado al usuario en Auth0 (por ejemplo <c>ADMIN</c>); vacío si no tiene rol o aún no se ha sincronizado.</param>
    public sealed record BasicUserData(String Id, String Document, String FullName, String Phone, String Email, String RoleName = "");
}
