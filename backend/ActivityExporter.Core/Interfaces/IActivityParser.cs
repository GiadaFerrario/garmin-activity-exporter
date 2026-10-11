using ActivityExporter.Core.Models;
using ActivityExporter.Core.Exceptions;

namespace ActivityExporter.Core.Interfaces;

public interface IActivityParser
{
    /// <summary>
    /// Parses the uploaded file
    /// </summary>
    /// <param name="stream">Content of the .fit file</param>
    /// <returns>A summary of the activity described in the file with a list containing splits</returns>
    /// <exception cref="InvalidFileException"> When the file is not valid or the sport is not supported</exception>
    ActivitySummary Parse(Stream stream);
}