using System.Text.Json;
using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using Connection360.Domain.Dtos;
using Connection360.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Connection360.Application.Tests.DTOs
{
    public class GatewayAndShipmentDtosTests
    {
        [Fact]
        public void ApplicationDataSheetDataRequest_ValoresPorDefecto_SonTodosSinFiltro()
        {
            var dto = new ApplicationDataSheetDataRequest();

            dto.Scope.Should().Be(ApplicationDataSheetViewScope.Todos);
            dto.NitCliente.Should().BeNull();
            dto.FieldsSelection.Should().BeNull();
        }

        [Fact]
        public void ApplicationDataSheetDataRequest_DebeAsignarPropiedades()
        {
            var selection = new ApplicationDataSheetViewFieldsSelectionDto { Fields = { ApplicationDataSheetViewField.Id } };
            var dto = new ApplicationDataSheetDataRequest { Scope = ApplicationDataSheetViewScope.Entregados, NitCliente = "900", FieldsSelection = selection };

            dto.Scope.Should().Be(ApplicationDataSheetViewScope.Entregados);
            dto.NitCliente.Should().Be("900");
            dto.FieldsSelection.Should().BeSameAs(selection);
        }

        [Fact]
        public void LogStatusTrackingDataRequest_ValoresPorDefecto_SonNulos()
        {
            var dto = new LogStatusTrackingDataRequest();

            dto.DocumentoTransporteHbl.Should().BeNull();
            dto.FieldsSelection.Should().BeNull();
        }

        [Fact]
        public void LogStatusTrackingDataRequest_DebeAsignarPropiedades()
        {
            var selection = new LogStatusTrackingViewFieldsSelectionDto { Fields = { LogStatusTrackingViewField.Mensaje } };
            var dto = new LogStatusTrackingDataRequest { DocumentoTransporteHbl = "HBL-1", FieldsSelection = selection };

            dto.DocumentoTransporteHbl.Should().Be("HBL-1");
            dto.FieldsSelection.Should().BeSameAs(selection);
        }

        [Fact]
        public void ContainerShipmentsResponse_ValoresPorDefecto_SonCadenasVacias()
        {
            var dto = new ContainerShipmentsResponse();

            dto.ContainerType.Should().BeEmpty();
            dto.ContainerAmount.Should().BeEmpty();
            dto.ContainerNumber.Should().BeEmpty();
            dto.DaysOff.Should().BeEmpty();
            dto.DaysRemainingDelivery.Should().BeEmpty();
            dto.ActualContainerReturnDate.Should().Be(default);
            dto.ContainerDelayDays.Should().BeEmpty();
            dto.CostDayOfDelay.Should().BeEmpty();
            dto.TotalCostContainerDelays.Should().BeEmpty();
            dto.ContainerDepot.Should().BeEmpty();
        }

        [Fact]
        public void ContainerShipmentsResponse_DebeAsignarPropiedades()
        {
            var date = new DateTime(2025, 1, 1);
            var dto = new ContainerShipmentsResponse
            {
                ContainerType = "40HC", ContainerAmount = "2", ContainerNumber = "MSKU1", DaysOff = "7",
                DaysRemainingDelivery = "3", ActualContainerReturnDate = date, ContainerDelayDays = "1",
                CostDayOfDelay = "10", TotalCostContainerDelays = "10", ContainerDepot = "500"
            };

            dto.ContainerType.Should().Be("40HC");
            dto.ContainerAmount.Should().Be("2");
            dto.ContainerNumber.Should().Be("MSKU1");
            dto.DaysOff.Should().Be("7");
            dto.DaysRemainingDelivery.Should().Be("3");
            dto.ActualContainerReturnDate.Should().Be(date);
            dto.ContainerDelayDays.Should().Be("1");
            dto.CostDayOfDelay.Should().Be("10");
            dto.TotalCostContainerDelays.Should().Be("10");
            dto.ContainerDepot.Should().Be("500");
        }

        [Fact]
        public void DetailsHistoryShipmentsResponse_DebeAsignarYValoresPorDefecto()
        {
            var empty = new DetailsHistoryShipmentsResponse();
            empty.ChangeUser.Should().BeEmpty();
            empty.Message.Should().BeEmpty();
            empty.OldState.Should().BeEmpty();
            empty.NewState.Should().BeEmpty();
            empty.ChangeDate.Should().Be(default);

            var date = new DateTime(2025, 2, 3);
            var dto = new DetailsHistoryShipmentsResponse { ChangeDate = date, ChangeUser = "u", Message = "m", OldState = "a", NewState = "b" };
            dto.ChangeDate.Should().Be(date);
            dto.ChangeUser.Should().Be("u");
            dto.Message.Should().Be("m");
            dto.OldState.Should().Be("a");
            dto.NewState.Should().Be("b");
        }

        [Fact]
        public void FinancialInfoShipmentsResponse_DebeAsignarYValoresPorDefecto()
        {
            var empty = new FinancialInfoShipmentsResponse();
            empty.AdvancePaymentAmount.Should().BeEmpty();
            empty.SupplierInvoice.Should().BeEmpty();
            empty.TCCInvoice.Should().BeEmpty();
            empty.InvoiceNumber.Should().BeEmpty();
            empty.InvoiceDate.Should().BeEmpty();
            empty.ExpenseDescription.Should().BeEmpty();
            empty.ExpenseAmountUSD.Should().BeEmpty();
            empty.InvoiceSubtotalUSD.Should().BeEmpty();
            empty.IvaUSD.Should().BeEmpty();
            empty.TotalInvoiceUSD.Should().BeEmpty();

            var d1 = new DateTime(2025, 1, 1);
            var d2 = new DateTime(2025, 1, 2);
            var dto = new FinancialInfoShipmentsResponse
            {
                AdvancePaymentRequestDate = d1, AdvancePaymentDate = d2, AdvancePaymentAmount = "1", SupplierInvoice = "2",
                TCCInvoice = "3", InvoiceNumber = "4", InvoiceDate = "5", ExpenseDescription = "6", ExpenseAmountUSD = "7",
                InvoiceSubtotalUSD = "8", IvaUSD = "9", TotalInvoiceUSD = "10"
            };
            dto.AdvancePaymentRequestDate.Should().Be(d1);
            dto.AdvancePaymentDate.Should().Be(d2);
            dto.AdvancePaymentAmount.Should().Be("1");
            dto.SupplierInvoice.Should().Be("2");
            dto.TCCInvoice.Should().Be("3");
            dto.InvoiceNumber.Should().Be("4");
            dto.InvoiceDate.Should().Be("5");
            dto.ExpenseDescription.Should().Be("6");
            dto.ExpenseAmountUSD.Should().Be("7");
            dto.InvoiceSubtotalUSD.Should().Be("8");
            dto.IvaUSD.Should().Be("9");
            dto.TotalInvoiceUSD.Should().Be("10");
        }

        [Fact]
        public void HistoryShipmentsResponse_DetalleNulo_NoDebeSerializarLaPropiedad()
        {
            var dto = new HistoryShipmentsResponse();

            String json = JsonSerializer.Serialize(dto);

            dto.DetailsHistoryShipments.Should().BeNull();
            json.Should().Be("{}");
        }

        [Fact]
        public void HistoryShipmentsResponse_ConDetalle_DebeSerializarLaPropiedad()
        {
            var dto = new HistoryShipmentsResponse
            {
                DetailsHistoryShipments = new List<DetailsHistoryShipmentsResponse> { new() { ChangeUser = "u" } }
            };

            String json = JsonSerializer.Serialize(dto);

            json.Should().Contain("DetailsHistoryShipments").And.Contain("\"ChangeUser\":\"u\"");
        }

        [Fact]
        public void LogisticsDatesShipmentsResponse_DebeAsignarPropiedades()
        {
            DateTime D(Int32 n) => new DateTime(2025, 1, n);
            var dto = new LogisticsDatesShipmentsResponse
            {
                StoreOriginDate = D(1), ETDDate = D(2), ATDDate = D(3), ETADate = D(4), ATADate = D(5),
                StoreDestinationDate = D(6), NationalizationDate = D(7), DispatchDestinationDate = D(8),
                FormDate = D(9), ContainerDeliveryDate = D(10)
            };

            dto.StoreOriginDate.Should().Be(D(1));
            dto.ETDDate.Should().Be(D(2));
            dto.ATDDate.Should().Be(D(3));
            dto.ETADate.Should().Be(D(4));
            dto.ATADate.Should().Be(D(5));
            dto.StoreDestinationDate.Should().Be(D(6));
            dto.NationalizationDate.Should().Be(D(7));
            dto.DispatchDestinationDate.Should().Be(D(8));
            dto.FormDate.Should().Be(D(9));
            dto.ContainerDeliveryDate.Should().Be(D(10));
            new LogisticsDatesShipmentsResponse().ETDDate.Should().Be(default);
        }

        [Fact]
        public void SummaryShipmentsResponse_DebeAsignarYValoresPorDefecto()
        {
            var empty = new SummaryShipmentsResponse();
            new[]
            {
                empty.Id, empty.ClientId, empty.ClientName, empty.Supplier, empty.Carrier, empty.MerchandiseDescription,
                empty.DocumentNumber, empty.DocumentType, empty.Origin, empty.Destination, empty.LoadType,
                empty.PackagesNumbers, empty.WeightKg, empty.VolumeM3, empty.Incoterm, empty.OperationType, empty.ShipmentMode
            }.Should().OnlyContain(s => s == String.Empty);

            var dto = new SummaryShipmentsResponse
            {
                Id = "1", ClientId = "2", ClientName = "3", Supplier = "4", Carrier = "5", MerchandiseDescription = "6",
                DocumentNumber = "7", DocumentType = "8", Origin = "9", Destination = "10", LoadType = "11",
                PackagesNumbers = "12", WeightKg = "13", VolumeM3 = "14", Incoterm = "15", OperationType = "16", ShipmentMode = "17"
            };
            dto.Id.Should().Be("1");
            dto.ClientId.Should().Be("2");
            dto.ClientName.Should().Be("3");
            dto.Supplier.Should().Be("4");
            dto.Carrier.Should().Be("5");
            dto.MerchandiseDescription.Should().Be("6");
            dto.DocumentNumber.Should().Be("7");
            dto.DocumentType.Should().Be("8");
            dto.Origin.Should().Be("9");
            dto.Destination.Should().Be("10");
            dto.LoadType.Should().Be("11");
            dto.PackagesNumbers.Should().Be("12");
            dto.WeightKg.Should().Be("13");
            dto.VolumeM3.Should().Be("14");
            dto.Incoterm.Should().Be("15");
            dto.OperationType.Should().Be("16");
            dto.ShipmentMode.Should().Be("17");
        }

        [Fact]
        public void CreateOutboxMessagesDto_ConFechaExplicita_DebeConservarla()
        {
            var date = new DateTime(2025, 1, 2, 3, 4, 5);
            var dto = new CreateOutboxMessagesDto("c", "e", "d", "t", "m", date);

            dto.ClientId.Should().Be("c");
            dto.EventType.Should().Be("e");
            dto.DocumentNumber.Should().Be("d");
            dto.Title.Should().Be("t");
            dto.Message.Should().Be("m");
            dto.MessageDate.Should().Be(date);
        }

        [Fact]
        public void CreateOutboxMessagesDto_SinFecha_DebeUsarLaFechaActual()
        {
            DateTime before = DateTime.Now;
            var dto = new CreateOutboxMessagesDto("c", "e", "d", "t", "m");
            DateTime after = DateTime.Now;

            dto.MessageDate.Should().NotBeNull();
            dto.MessageDate!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        }

        [Fact]
        public void CreateOutboxMessagesDto_EsUnRecordConIgualdadPorValor()
        {
            var date = new DateTime(2025, 1, 1);
            var a = new CreateOutboxMessagesDto("c", "e", "d", "t", "m", date);
            var b = new CreateOutboxMessagesDto("c", "e", "d", "t", "m", date);

            a.Should().Be(b);
            (a with { Title = "otro" }).Should().NotBe(a);
        }

        [Fact]
        public void CustomerNotificationEventsResponseDto_EsUnRecordConIgualdadPorValor()
        {
            var a = new CustomerNotificationEventsResponseDto(1, 2, true, false, true, false, true);
            var b = new CustomerNotificationEventsResponseDto(1, 2, true, false, true, false, true);

            a.Should().Be(b);
            a.IdNotificationEvent.Should().Be(1);
            a.IdCustomer.Should().Be(2);
            a.ChangeState.Should().BeTrue();
            a.SuccessfulDelivery.Should().BeFalse();
            a.WithIssues.Should().BeTrue();
            a.ShipmentTransit.Should().BeFalse();
            a.DeliveryReminder.Should().BeTrue();
            (a with { ChangeState = false }).Should().NotBe(a);
        }
    }
}
