using Connection360.Domain.Constans;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Interfaces;

namespace Connection360.Domain.Services
{
    public class DetailsHistoryShipmentsDomainService : IDetailsHistoryShipmentsDomainService
    {
        public HistoryShipmentsDomainDtoResult GetDetailsHistoryShipments(DynamicDataSet dataSet, String DocumentNumber)
        {
            //List<DynamicRecord> historyRecords = dataSet.Rows
            //    .Where(r => r[ExternalDataFields.DocumentNumber] == DocumentNumber)
            //    .ToList();

            List<DetailsHistoryShipmentsDomainDtoResult> historyShipmentsResult = dataSet.Rows
                .OrderBy(r => Int64.TryParse(r[ExternalDataFields.IdLog], out Int64 id) ? id : 0)
                .Select(s => new DetailsHistoryShipmentsDomainDtoResult // Mapeo a tu DTO final
                {
                    ChangeDate = s[ExternalDataFields.ChangeDateLog].ToDateTimeOrMin(),
                    ChangeUser = s[ExternalDataFields.ChangeUserLog],
                    Message = s[ExternalDataFields.MessageLog],
                    OldState = s[ExternalDataFields.OldStateLog],
                    NewState = s[ExternalDataFields.NewStateLog]
                })
                .ToList();

            return new HistoryShipmentsDomainDtoResult
            {
                DetailsHistoryShipments = historyShipmentsResult
            };
        }
    }
}
