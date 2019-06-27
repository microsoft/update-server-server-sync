---------------- Microsoft Update Services Server-Server Sync library -----------------

See https://github.com/microsoft/update-server-server-sync/wiki/Library-examples for more examples.

Quickstart: sync metadata from the official Microsoft upstream server:

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UpdateServices;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Query;

static async Task SyncUpdates()
{
    // Create client
    var server = new Microsoft.UpdateServices.UpstreamServerClient(Endpoint.Default);

    // Sync categories
    var categories = await server.GetCategories();

    // Print category titles
    categories.Updates.ForEach(cat => Console.WriteLine(cat.Title));

    // Create a filter for sync'ing updates
    var filter = new QueryFilter(
                         categories.Updates.OfType<MicrosoftProduct>().Take(1),
                         categories.Updates.OfType<Classification>());

    // Get updates
    var updates = await server.GetUpdates(filter);

    // Print update titles
    updates.Updates.ForEach(update => Console.WriteLine(update.Title));
}