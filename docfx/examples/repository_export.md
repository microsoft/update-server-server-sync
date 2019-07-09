Exporting updates from a local repository to WSUS.

Assumes the repository has been initialized and sync'ed with an upstream server.
```
// Open a repository that was previously sync'ed with the desired updates
var localRepo = FileSystemRepository.Open(Environment.CurrentDirectory);

// Export all updates that have "Surface firmware" in their title to a WSUS compatible format
localRepo.Export(
    new RepositoryFilter() { TitleFilter = "Surface firmware" },
    "export.cab",
    RepoExportFormat.WSUS_2016);
```

Copy "export.cab" to the WSUS server, and from an elevated command line run the wsusutil.exe utility:
```
"C:\Program Files\Update Services\Tools\WsusUtil.exe" import export.cab log.xml
```
