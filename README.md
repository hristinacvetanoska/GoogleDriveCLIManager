# Google Drive CLI Manager

A command-line interface (CLI) tool built with **.NET 8** that interacts with the Google Drive API. It allows users to authenticate via OAuth 2.0, synchronize files locally using parallel processing, search for files with sync status indicators, and upload local files to Google Drive.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A Google account
- A Google Cloud project with the Drive API enabled (see below)

---

## Google Cloud Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project
3. Navigate to **APIs & Services → Library**
4. Search for **Google Drive API** and click **Enable**
5. Navigate to **APIs & Services → Credentials**
6. Click **Create Credentials → OAuth 2.0 Client ID**
7. If prompted, configure the OAuth Consent Screen:
   - Choose **External**
   - Fill in the app name and your email
   - Under **Test Users**, add your Gmail address
8. Set application type to **Desktop App** and click **Create**
9. Click **Download JSON** and rename the file to `client_secret.json`

---

## Configuration

> ⚠️ This step is required before running any command.

Place your `client_secret.json` file in the **project root directory** — the same folder as `GoogleDriveCLIManager.csproj`:

```
GoogleDriveCLIManager/
├── client_secret.json        ← place it here
├── GoogleDriveCLIManager.csproj
├── Program.cs
└── ...
```

On **first run**, a browser window will open asking you to log in with your Google account and grant Drive access. After authenticating, the token is saved automatically to `~/.google-drive-cli/token/` and you will **not** need to authenticate again on subsequent runs.

---

## Installation & Build

Clone the repository:

```bash
git clone https://github.com/hristinacvetanoska/GoogleDriveCLIManager.git
cd GoogleDriveCLIManager/GoogleDriveCLIManager
```

Restore dependencies and build:

```bash
dotnet restore
dotnet build
```

---

## Usage

All commands are run from the project root directory (where the `.csproj` file is located).

---

### `sync` — Download all files from Google Drive

Downloads all files from your Google Drive (owned by you) to a local `Downloads/` folder. Files that have already been downloaded are automatically skipped.

```bash
dotnet run -- sync
```

Example output:
```
Starting Google Drive sync...
✓ Found 5 files on Google Drive.
Downloading 3 new files...

Downloading files ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100%
               
                Sync Statistics
╭──────────────────────────────────────────╮
│ Metric                   │  Value
├──────────────────────────┬───────────────┤
│ Total files on Drive     │ 5             │
│ Successfully downloaded  │ 3             │
│ Skipped (already synced) │ 2             │
│ Failed                   │ 0             │
│ Total data downloaded    │ 12.45 MB      │
│ Time elapsed             │ 00:08.32      │
╰──────────────────────────┴───────────────╯
```

---

### `search` — Search for files by name

Searches your entire Google Drive by filename and shows the sync status of each result.

```bash
dotnet run -- search "report"
```

Example output:
```
Searching Google Drive for: report

Found 2 result(s):

                        Search Results
╭────────────────────────────────────────────────────────────────────────────╮
│  Name            │ Type         │ Size    │ Modified   │ Status            |                                                        │
├──────────────────┼──────────────┼─────────┼────────────┼───────────────────┤
│ Q3 Report.docx   │ 📝 Google Doc│ N/A     │ 2024-01-10 │ ✓ Synced         │
│ Final Report.pdf │ 📄 PDF       │ 2.45 MB │ 2024-01-08 │ ✗ Not Downloaded │
╰──────────────────┴──────────────┴─────────┴────────────┴───────────────────╯
```

---

### `upload` — Upload a local file to Google Drive

Uploads a file from your local machine to a specific folder path on Google Drive. If the target folder path does not exist, it will be **created automatically**.

```bash
# Upload to a specific folder path
dotnet run -- upload "C:\Users\Username\Desktop\report.pdf" "Test/Reports/2026"

# Upload directly to My Drive root
dotnet run -- upload "C:\Users\Username\Desktop\report.pdf"
```

Example output:
```
Uploading file: report.pdf
Destination:    Test/Reports/2026
Size:           2.45 MB

✓ Destination folder ready.
Uploading report.pdf ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100%

              Upload Successful
╭──────────────────────────────────────────────╮
│ Detail           │ Values
├──────────────────┬───────────────────────────┤
│ File name        │ report.pdf                │
│ Drive path       │ Test/Reports/2026         │
│ File size        │ 2.45 MB                   │
│ Drive file ID    │ 1BxiMVs0XRA5nFMdKvBd...  │
╰──────────────────┴───────────────────────────╯
```

---

## Architecture

This project follows a **Simplified Clean Architecture** pattern.
Dependencies flow inward — outer layers depend on inner layers, never the reverse.
```
┌─────────────────────────────────────────────────────────────────┐
│                       Commands Layer                            │
│  SyncCommand, SearchCommand, UploadCommand                      │
│  Handles CLI parsing and user-facing output only.               │
│  Depends on Application layer interfaces.                       │
├─────────────────────────────────────────────────────────────────┤
│                      Application Layer                          │
│  AuthService, GoogleDriveService, FileSystemService             │
│  Contains all business logic and use case orchestration.        │
│  Defines interfaces that outer layers implement.                │
├─────────────────────────────────────────────────────────────────┤
│                     Infrastructure Layer                        │
│  ManifestRepository, TypeRegistrar                              │
│  Handles external concerns — file I/O and state persistence.    │
│  Implements interfaces defined in the Application layer.        │
├─────────────────────────────────────────────────────────────────┤
│                       Domain Layer                              │
│  DriveFileInfo, SyncManifest, SyncStatistics, ManifestEntry     │
│  Core models with no external dependencies.                     │
│  Every other layer depends on this — this depends on nothing.   │
└─────────────────────────────────────────────────────────────────┘
````
**Why Simplified Clean Architecture?**
Full Clean Architecture includes additional layers such as separate Use Cases 
and dedicated project per layer. For a CLI tool of this scope, that would be 
over-engineered. The simplified version preserves the core principles — 
separation of concerns and dependency inversion — without unnecessary complexity.

---

## Design Decisions

### Parallel Downloads — `ActionBlock<T>`

The `sync` command uses `ActionBlock<T>` from TPL Dataflow for parallel file downloading:

```csharp
var downloadBlock = new ActionBlock<DriveFileInfo>(
    async file => await DownloadFileAsync(file, manifest, progressTask, cancellationToken),
    new ExecutionDataflowBlockOptions
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount * 2
    });
```

**Why `ActionBlock` over alternatives?**

| Approach | Problem |
|---|---|
| `Task.WhenAll` | Starts ALL tasks at once — risks rate limiting and memory exhaustion |
| `Parallel.ForEachAsync` | Good, but no backpressure — all items queued in memory at once |
| `ActionBlock<T>` ✅ | Bounded parallelism + built-in backpressure — controls exactly how many concurrent downloads run |

Since downloading is **I/O-bound** (not CPU-bound), `ProcessorCount * 2` is used to maximize throughput without overloading the system.

---

### Thread-Safe Statistics — `Interlocked`

Success/failure counters are updated from multiple parallel threads using `Interlocked` instead of `lock`:

```csharp
Interlocked.Increment(ref _successCount);
Interlocked.Add(ref _totalBytesDownloaded, fileSize);
```

**Why `Interlocked` over `lock`?**
- Lock-free atomic operations — no thread contention
- Faster than `lock` for simple counter increments
- No risk of deadlocks

For the failed files collection, `ConcurrentBag<string>` is used — a thread-safe collection designed for concurrent additions from multiple threads.

---

### Thread-Safe Manifest Updates — `lock` + `SemaphoreSlim`

During parallel sync, multiple threads update the manifest dictionary and write to disk simultaneously.

- `lock(manifest.Entries)` — protects in-memory dictionary updates
- `SemaphoreSlim(1,1)` — ensures only one thread writes the JSON file at a time

**Why `SemaphoreSlim` instead of `lock` for file writing?**

`lock` cannot be used with `await` inside it. `SemaphoreSlim.WaitAsync()` is async-friendly — it waits without blocking the thread.

---

### Sync State — Manifest File

The `search` command determines if a file is downloaded by checking a local `manifest.json` file stored in `Downloads/manifest.json`:

```json
{
  "lastSyncTime": "2024-01-15T10:30:00Z",
  "entries": {
    "fileId123": {
      "fileName": "resume.pdf",
      "localPath": "Downloads/resume.pdf",
      "downloadedAt": "2024-01-15T10:30:00Z",
      "fileSizeBytes": 204800
    }
  }
}
```

**Why manifest over filesystem scan?**
- O(1) lookup by Drive file ID vs O(n) filesystem scan
- Handles renamed files correctly
- Handles duplicate filenames correctly
- Persists additional metadata (download time, file size)

---

### Google Drive Service — Facade Pattern

`GoogleDriveService` acts as a **Facade** over the Google SDK, centralizing all API interactions, pagination, and retry logic behind a clean interface:

```
Commands → IGoogleDriveService → GoogleDriveService → Google SDK
```

This decouples commands from Google SDK implementation details entirely.

---

### Error Handling — Exponential Backoff Retry

All Google API calls are wrapped in a retry mechanism that handles rate limiting (HTTP 429) and service unavailability (HTTP 503):

```
Attempt 1 fails → wait 1s
Attempt 2 fails → wait 2s
Attempt 3 fails → wait 4s
Attempt 4 fails → throw
```

---

## Project Structure

```
GoogleDriveCLIManager/
├── Commands/
│   ├── SyncCommand.cs
│   ├── SearchCommand.cs
│   └── UploadCommand.cs
├── Services/
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── IGoogleDriveService.cs
│   │   ├── IFileSystemService.cs
│   │   └── IManifestRepository.cs
│   ├── AuthService.cs
│   ├── GoogleDriveService.cs
│   └── FileSystemService.cs
├── Models/
│   ├── DriveFileInfo.cs
    ├── ManifestEntry.cs
│   ├── SyncStatistics.cs
│   └── SyncManifest.cs
├── Infrastructure/
│   ├── Exceptions/
│   │   └── DriveException.cs
│   ├── ManifestRepository.cs
│   └── TypeRegistrar.cs
├── Helpers/
│   ├── FileSizeFormatter.cs
│   ├── MarkupHelper.cs
│   └── MimeTypeHelper.cs
├── Program.cs
└── README.md

GoogleDriveCLIManager.Tests/
├── Commands/
│   ├── SyncCommandTests.cs
│   ├── SearchCommandTests.cs
│   └── UploadCommandTests.cs
├── Helpers/
│   ├── FileSizeFormatterTests.cs
│   └── MimeTypeHelperTests.cs
├── Infrastructure/
│   └── ManifestRepositoryTests.cs
├── Services/
│   └── FileSystemServiceTests.cs
└── TestCollections.cs
```

---

## Running Tests

```bash
cd GoogleDriveCLIManager.Tests
dotnet test
```
