using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;

namespace Microsoft.UpdateServices.Server
{
    /// <summary>
    /// MVC controller for handling content requests on an upstream updates server.
    /// </summary>
    public class ContentController : Controller
    {
        IRepository LocalRepository;
        Dictionary<string, UpdateFile> UpdateFiles;

        internal ContentController(IRepository localRepo, RepositoryFilter filter)
        {
            LocalRepository = localRepo;

            var updatesWithFiles = LocalRepository.GetUpdates(filter, UpdateRetrievalMode.Extended).OfType<IUpdateWithFiles>();

            UpdateFiles = updatesWithFiles.SelectMany(u => u.Files).Distinct().ToDictionary(
                f => $"{f.GetContentDirectoryName().ToLower()}/{f.FileName.ToLower()}");
        }
        /// <summary>
        /// Handle HTTP get requests on the Content/(Directory)/(FileName) URLs
        /// </summary>
        /// <param name="directory">The directory name for an update file</param>
        /// <param name="name">The file name for an update file</param>
        /// <returns>File content on success, other error codes otherwise</returns>
        [HttpGet("Content/{directory}/{name}", Name = "GetUpdateContent")]
        public IActionResult GetUpdateContent(string directory, string name)
        {
            var lookupKey = $"{directory.ToLower()}/{name.ToLower()}";

            if (UpdateFiles.TryGetValue(lookupKey, out UpdateFile file))
            {
                if (!LocalRepository.IsFileDownloaded(file))
                {
                    return NotFound();
                }

                return new FileStreamResult(LocalRepository.GetUpdateFileStream(file), "application/octet-stream");
            }
            else
            {
                return NotFound();
            }
        }
    }
}
