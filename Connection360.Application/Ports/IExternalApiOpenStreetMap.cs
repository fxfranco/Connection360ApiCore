using Connection360.Application.DTOs;

namespace Connection360.Application.Ports
{
    public interface IExternalApiOpenStreetMap
    {
        public Task<OpenStreetMapDto> GetCoordinates(String apiName, String PlaceName, CancellationToken cancellationToken);
    }
}
