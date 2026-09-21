# WARDOGS Coordinate Calculator

Lightweight C# Windows Forms app. Two coordinate text inputs; large distance,
bearing and compass direction outputs. Offline, event-driven; no polling,
web browser, screen capture, telemetry, or game integration.

## Run on Windows

Install the .NET 8 SDK or a newer SDK capable of targeting .NET 8.
Open a terminal in this extracted folder:

```powershell
dotnet run --project WardogsCalculator.csproj
```

Or open WardogsCalculator.csproj in Visual Studio with the .NET desktop workload.

## Make a standalone Windows x64 executable

With the SDK installed, double-click **Build-Windows.cmd**. It publishes a
self-contained executable and runs the included C# self-tests. Internet is
required for the first build to restore dependencies. The result is
`publish/WardogsCalculator.exe`; copy it anywhere and double-click to run.
No installer, administrator privileges, or separate .NET runtime is needed
on the destination PC. The SDK is required only on the build PC.

Equivalent publish command:

```powershell
dotnet publish WardogsCalculator.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Run publish/WardogsCalculator.exe. The standalone bundle includes .NET and is
larger on disk than the source. File size is not a RAM usage measurement.

## Use

Paste one coordinate pair into A and one into B using Ctrl+V or the Paste buttons.
Examples: `x98.43, y110.38`, `A= x98.43, y110.38`, `X:98.43 Y:110.38`.
Decimal dots and commas are both accepted: `x98.43, y110,38` and
`x98,43, y110,38` mean the same coordinates. Only numeric decimal commas
are normalized; the separator between X and Y remains intact. Spaces, signs,
case differences, optional labels, and comma/semicolon separators are supported.
Outputs refresh on input changes. Invalid text clears old results.

Scale is 100 metres per coordinate unit, matching the supplied reference tool.
X increases eastward. Y defaults northward; select "Y increases southward" if
that matches the game's map. Confirm this orientation against a known pair of
positions before relying on the bearing. Bearings are clockwise from north.
Directions use eight compass sectors. Coincident positions have no direction.

Sample A `x98.43, y110.38`, B `x94.53, y109.03`:
412.7045 m, approximately 250.9 degrees, W with Y northward.
With Y southward: approximately 289.1 degrees, W. Distance is unchanged.

Always on top is a normal Windows window; use borderless/windowed gameplay or a
second monitor. Exclusive fullscreen may hide it. No injected game overlay.
Mortar elevation is not included: it needs weapon-specific game calibration.

## Verification

Included self-tests cover the sample, parsing failures, cardinal bearings,
Y-axis reversal, and coincident locations. On Windows:

```powershell
dotnet run --project WardogsCalculator.csproj -- --self-test
```

Successful tests exit without showing a window; failures throw an exception.
The Windows x64 executable was cross-compiled successfully with .NET SDK
8.0.425. The actual C# parsing and calculation self-tests passed in a Linux
console harness. The Windows interface has not been run or visually tested
here. Resource usage has not been benchmarked.
