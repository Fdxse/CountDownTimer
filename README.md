# CountDownTimer

A small on-screen countdown timer for Windows. Only the digits are shown: the window
has no border, a transparent background, and always stays on top of other windows.
When the time is up it flashes a red **00:00** until you close or restart it.

## Requirements

- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (to build), or the .NET 8 Desktop Runtime (to run a built copy)

No external NuGet packages are used.

## Run it

```powershell
dotnet run --project src/CountDownTimer
```

A small dialog asks how long to count down. You can also pass the duration directly:

```powershell
dotnet run --project src/CountDownTimer -- 4:30
```

### Durations you can type

| You type   | Means                    |
|------------|--------------------------|
| `5`        | 5 minutes                |
| `1.5`      | 1 minute 30 seconds      |
| `4:30`     | 4 minutes 30 seconds     |
| `1:15:00`  | 1 hour 15 minutes        |
| `90s`      | 90 seconds               |
| `1h 30m`   | 1 hour 30 minutes        |

## Controls

| Action                 | What it does                         |
|------------------------|--------------------------------------|
| Drag with left mouse   | Move the timer                       |
| Mouse wheel            | Make the digits bigger or smaller    |
| `Space`                | Pause / resume (digits dim when paused) |
| `R`                    | Restart with the same duration       |
| `N`                    | Set a new duration                   |
| `Esc`                  | Close                                |
| Right-click            | Menu with the same actions           |

## Make a standalone .exe

```powershell
dotnet publish src/CountDownTimer -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

The exe ends up in `src/CountDownTimer/bin/Release/net8.0-windows/win-x64/publish/`.
