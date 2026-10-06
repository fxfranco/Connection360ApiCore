using Connection360.Etl.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Domain.Test.Enums
{
    public class DataSetJoinTypeTests
    {
        [Fact]
        public void Valores_TienenLosValoresNumericosEsperados()
        {
            ((Int32)DataSetJoinType.Inner).Should().Be(0);
            ((Int32)DataSetJoinType.FullOuter).Should().Be(1);
            Enum.GetValues<DataSetJoinType>().Should().HaveCount(2);
        }
    }

    public class EtlChangeEventTypeTests
    {
        [Theory]
        [InlineData(EtlChangeEventType.ChangeState, "ChangeState")]
        [InlineData(EtlChangeEventType.Comment, "Comment")]
        public void ToDbValue_ValorDefinido_RetornaTextoDeBD(EtlChangeEventType tipo, String esperado)
        {
            tipo.ToDbValue().Should().Be(esperado);
        }

        [Fact]
        public void ToDbValue_ValorNoDefinido_LanzaArgumentOutOfRange()
        {
            Action act = () => ((EtlChangeEventType)999).ToDbValue();

            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("eventType");
        }

        [Fact]
        public void ToDbValue_TodosLosValores_TienenMapeoNoVacio()
        {
            foreach (var v in Enum.GetValues<EtlChangeEventType>())
                v.ToDbValue().Should().NotBeNullOrWhiteSpace();
        }
    }

    public class EtlJobNameTests
    {
        [Theory]
        [InlineData(EtlJobName.ApplicationDataSheet, "application_data_sheet")]
        [InlineData(EtlJobName.LogStatusTracking, "log_status_tracking")]
        [InlineData(EtlJobName.ApplicationDataSheetMigration, "application_data_sheet_migration")]
        public void ToDbValue_ValorDefinido_RetornaTextoDeBD(EtlJobName nombre, String esperado)
        {
            nombre.ToDbValue().Should().Be(esperado);
        }

        [Fact]
        public void ToDbValue_ValorNoDefinido_LanzaArgumentOutOfRange()
        {
            Action act = () => ((EtlJobName)999).ToDbValue();

            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("jobName");
        }

        [Theory]
        [InlineData("application_data_sheet", EtlJobName.ApplicationDataSheet)]
        [InlineData("log_status_tracking", EtlJobName.LogStatusTracking)]
        [InlineData("application_data_sheet_migration", EtlJobName.ApplicationDataSheetMigration)]
        public void ToEtlJobName_TextoConocido_RetornaEnum(String texto, EtlJobName esperado)
        {
            texto.ToEtlJobName().Should().Be(esperado);
        }

        [Theory]
        [InlineData("desconocido")]
        [InlineData("")]
        [InlineData("APPLICATION_DATA_SHEET")]
        public void ToEtlJobName_TextoDesconocido_LanzaArgumentOutOfRange(String texto)
        {
            Action act = () => texto.ToEtlJobName();

            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("dbValue");
        }

        [Fact]
        public void ToEtlJobName_Nulo_LanzaArgumentOutOfRange()
        {
            Action act = () => ((String)null!).ToEtlJobName();

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void IdaYVuelta_TodosLosValores_SonSimetricos()
        {
            foreach (var v in Enum.GetValues<EtlJobName>())
                v.ToDbValue().ToEtlJobName().Should().Be(v);
        }
    }

    public class EtlJobStatusTests
    {
        [Theory]
        [InlineData(EtlJobStatus.Processing, "PROCESSING")]
        [InlineData(EtlJobStatus.Completed, "COMPLETED")]
        [InlineData(EtlJobStatus.Failed, "FAILED")]
        public void ToDbValue_ValorDefinido_RetornaTextoDeBD(EtlJobStatus estado, String esperado)
        {
            estado.ToDbValue().Should().Be(esperado);
        }

        [Fact]
        public void ToDbValue_ValorNoDefinido_LanzaArgumentOutOfRange()
        {
            Action act = () => ((EtlJobStatus)999).ToDbValue();

            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("status");
        }

        [Theory]
        [InlineData("PROCESSING", EtlJobStatus.Processing)]
        [InlineData("COMPLETED", EtlJobStatus.Completed)]
        [InlineData("FAILED", EtlJobStatus.Failed)]
        public void ToEtlJobStatus_TextoConocido_RetornaEnum(String texto, EtlJobStatus esperado)
        {
            texto.ToEtlJobStatus().Should().Be(esperado);
        }

        [Theory]
        [InlineData("processing")]
        [InlineData("")]
        [InlineData("OTHER")]
        public void ToEtlJobStatus_TextoDesconocido_LanzaArgumentOutOfRange(String texto)
        {
            Action act = () => texto.ToEtlJobStatus();

            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("dbValue");
        }

        [Fact]
        public void IdaYVuelta_TodosLosValores_SonSimetricos()
        {
            foreach (var v in Enum.GetValues<EtlJobStatus>())
                v.ToDbValue().ToEtlJobStatus().Should().Be(v);
        }
    }
}
