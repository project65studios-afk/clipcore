using System.IO;
using System.Threading.Tasks;

namespace Project65.Core.Interfaces;

public interface IVisionService
{
    Task<string[]> AnalyzeImageAsync(Stream imageStream);
    Task<string> GenerateBatchSummaryAsync(IEnumerable<string> imageUrls);
}
