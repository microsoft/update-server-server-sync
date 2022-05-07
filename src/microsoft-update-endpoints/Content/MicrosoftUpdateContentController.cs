// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using System;
using System.Linq;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.Content
{
    /// <summary>
    /// ASP.NET controller; handles web requests for Microsoft Update content.
    /// <para>When added to a ASP.NETCore instance, routes and handles requests for Microsoft Update content</para>
    /// </summary>
    public class MicrosoftUpdateContentController : Controller
    {
        readonly IContentStore ContentStore;
        readonly ILogger ContentLogger;

        /// <summary>
        /// Create a new Microsoft Update content controller from the specified content store
        /// </summary>
        /// <param name="contentStore">The content store that has the update content</param>
        /// <param name="loggerFactory">Logger factory</param>
        public MicrosoftUpdateContentController(IContentStore contentStore, ILoggerFactory loggerFactory)
        {
            ContentLogger = loggerFactory.CreateLogger("Update GET");
            ContentStore = contentStore;
        }

        private static byte[] HexStringToHex(string inputHex)
        {
            var resultantArray = new byte[inputHex.Length / 2];
            for (var i = 0; i < resultantArray.Length; i++)
            {
                resultantArray[i] = System.Convert.ToByte(inputHex.Substring(i * 2, 2), 16);
            }
            return resultantArray;
        }

        internal static ContentFileDigest GetContentFileDigestFromUriPart(string name)
        {
            byte[] hashHex = HexStringToHex(name);
            if (hashHex.Length == 32)
            {
                return  new ContentFileDigest("SHA256", Convert.ToBase64String(hashHex));
            }
            else if (hashHex.Length == 20)
            {
                return new ContentFileDigest("SHA1", Convert.ToBase64String(hashHex));
            }
            else
            {
                throw new ArgumentException("Name is not valid hash hex string", nameof(name));
            }
        }

        /// <summary>
        /// Handles HTTP GET requests for update content.
        /// </summary>
        /// <param name="contentHash">The hash of the content being requested</param>
        /// <returns>Standard HTTP action result</returns>
        [HttpGet]
        public IActionResult GetMicrosoftUpdateContent(string contentHash)
        {
            if (ContentStore == null)
            {
                return NotFound();
            }

            ContentFileDigest parsedContentHash;
            try
            {
                parsedContentHash = GetContentFileDigestFromUriPart(contentHash);
            }
            catch(Exception)
            {
                return BadRequest();
            }

            if (ContentStore.Contains(parsedContentHash, out var fileName))
            {
                var typedHeaders = Request.GetTypedHeaders();
                if (typedHeaders.Range != null)
                {
                    ContentLogger.LogInformation($"Requested {fileName}, range {typedHeaders.Range.Ranges.First().From} -> {typedHeaders.Range.Ranges.First().To}");
                }
                else
                {
                    ContentLogger.LogInformation($"Requested {fileName}, no ranges");
                }
                return new FileStreamResult(ContentStore.Get(parsedContentHash), "application/octet-stream")
                {
                    FileDownloadName = fileName,
                    EnableRangeProcessing = true,
                };
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Handles HTTP HEAD request for update content. HEAD requests are send by clients to discover the size
        /// of the update content before proceeding with the download
        /// </summary>
        /// <param name="contentHash"></param>
        [HttpHead]
        public void GetMicrosoftUpdateContentHead(string contentHash)
        {
            if (ContentStore == null)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            ContentFileDigest parsedContentHash;
            try
            {
                parsedContentHash = GetContentFileDigestFromUriPart(contentHash);
            }
            catch (Exception)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (ContentStore.Contains(parsedContentHash, out var fileName))
            {
                ContentLogger.LogInformation($"HEAD {fileName}");

                using (var contentStream = ContentStore.Get(parsedContentHash))
                {
                    HttpContext.Response.ContentLength = contentStream.Length;
                }

                HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
        }
    }
}
