// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.UpdateServices.Server
{
    /// <summary>
    /// MVC controller for handling content requests on an upstream updates server.
    /// </summary>
    public class ContentController : Controller
    {
        IMetadataSource MetadataSource;
        IUpdateContentSource ContentSource;
        Dictionary<string, UpdateFile> UpdateFiles;

        internal ContentController(IMetadataSource metadataSource, IUpdateContentSource contentSource, MetadataFilter filter)
        {
            MetadataSource = metadataSource;
            ContentSource = contentSource;

            var updatesWithFiles = MetadataSource.GetUpdates(filter).Where(u => u.HasFiles);

            UpdateFiles = updatesWithFiles.SelectMany(u => u.Files).Distinct().ToDictionary(
                f => $"{f.GetContentDirectoryName().ToLower()}/{f.Digests[0].HexString.ToLower() + System.IO.Path.GetExtension(f.FileName).ToLower()}");
        }

        /// <summary>
        /// Handle HTTP GET requests on the Content/(Directory)/(FileName) URLs
        /// </summary>
        /// <param name="directory">The directory name for an update file</param>
        /// <param name="name">The file name for an update file</param>
        /// <returns>File content on success, other error codes otherwise</returns>
        [HttpGet("Content/{directory}/{name}", Name = "GetUpdateContent")]
        public IActionResult GetUpdateContent(string directory, string name)
        {
            var lookupKey = $"{directory.ToLower()}/{name.ToLower()}";

            if (UpdateFiles.TryGetValue(lookupKey, out UpdateFile file) &&
                 ContentSource.Contains(file))
            {
                var request = HttpContext.Request;

                var fileResult = new FileStreamResult(ContentSource.Get(file), "application/octet-stream");
                fileResult.FileDownloadName = name;
                fileResult.EnableRangeProcessing = true;
                return fileResult;
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Handle HTTP HEAD requests on the Content/(Directory)/(FileName) URLs
        /// </summary>
        /// <param name="directory">The directory name for an update file</param>
        /// <param name="name">The file name for an update file</param>
        /// <returns>File header on success, other error codes otherwise</returns>
        [HttpHead("Content/{directory}/{name}", Name = "GetUpdateContentHead")]
        public void GetUpdateContentHead(string directory, string name)
        {
            HttpContext.Response.Body = null;

            var lookupKey = $"{directory.ToLower()}/{name.ToLower()}";

            if (UpdateFiles.TryGetValue(lookupKey, out UpdateFile file) &&
                ContentSource.Contains(file))
            {
                var okResult = new OkResult();

                using (var contentStream = ContentSource.Get(file))
                {
                    HttpContext.Response.ContentLength = contentStream.Length;
                }

                HttpContext.Response.Body = null;
                HttpContext.Response.StatusCode = 200;
            }
            else
            {
                HttpContext.Response.StatusCode = 404;
            }
        }
    }
}
