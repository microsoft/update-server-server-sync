The following example shows how to query updates from a repository.

Assumes the repository has been initialized and sync'ed with an upstream server.
```
// Open a repository
var localRepository = FileSystemRepository.Open(Environment.CurrentDirectory);

// Get the classification ID for "Drivers"
var driversClassificationId = localRepository
  .ClassificationsIndex
  .Values
  .Where(c => c.Title.Equals("Drivers"))
  .Select(c => c.Identity.ID)
  .ToList();

// Query all drivers in the repository
var driverUpdates = localRepository.GetUpdates(
  new RepositoryFilter() { ClassificationFilter = driversClassificationId },
  UpdateRetrievalMode.Extended);
            
// Print the ID and title for each driver update
driverUpdates.ForEach(d => Console.WriteLine($"{d.Identity} : {d.Title}"));
```
