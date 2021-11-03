using System.IO;
using System.Threading.Tasks;

namespace PoeShared.UI
{
    public interface IExceptionReportHandler
    {
        Task<string> Handle(FileInfo exceptionReport);
    }
}