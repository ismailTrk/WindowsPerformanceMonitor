using SystemAnalyzer.Core.Interfaces;
using SystemAnalyzer.Core.Models;

namespace SystemAnalyzer.Infrastructure.Exporters
{
    /// <summary>
    /// Exporter Factory Implementation
    /// Responsibility: Create appropriate exporter based on format
    /// Pattern: Factory Pattern, Service Locator Pattern
    /// </summary>
    public class ExporterFactory : IExporterFactory
    {
        private readonly IEnumerable<IDataExporter> _exporters;

        public ExporterFactory(IEnumerable<IDataExporter> exporters)
        {
            _exporters = exporters ?? throw new ArgumentNullException(nameof(exporters));
        }

        public IDataExporter GetExporter(ExportFormat format)
        {
            var exporter = _exporters.FirstOrDefault(e => e.SupportedFormat == format);
            
            if (exporter == null)
            {
                throw new NotSupportedException($"Export format '{format}' is not supported. " +
                    $"Supported formats: {string.Join(", ", GetSupportedFormats())}");
            }

            return exporter;
        }

        public IEnumerable<ExportFormat> GetSupportedFormats()
        {
            return _exporters.Select(e => e.SupportedFormat).Distinct();
        }
    }
}