# MSDF Atlas Generator NuGet Package - MVP Implementation Guide

## What We're Building

We're creating a **NuGet package** called `Olve.MsdfAtlasGen` that wraps the `msdf-atlas-gen` command-line tool so it can be easily used from C# code.

### The Problem We're Solving

**Currently:** To generate font atlases, we'd have to:
1. Manually download/build `msdf-atlas-gen` for each platform
2. Call it using `Process.Start()` with messy command-line arguments
3. Parse the JSON output ourselves
4. Handle cross-platform path differences
5. Manually manage the native binaries

**After this package:** Developers can just:
```csharp
dotnet add package Olve.MsdfAtlasGen

var result = AtlasGenerator.Generate(new AtlasConfig
{
    FontPath = "Roboto-Regular.ttf",
    Type = AtlasType.MSDF,
    Dimensions = (1024, 1024)
});
// Clean, typed API with no manual process spawning!
```

---

## Why We're Doing This

### 1. **Clean API for Our Asset Pipeline**
Our game's asset pipeline needs to generate font atlases at build time. Instead of maintaining messy shell scripts and process calls, we get a clean C# API.

### 2. **Cross-Platform Made Easy**
NuGet handles including the correct native binary (Windows/Linux) automatically. No manual platform detection needed.

### 3. **Reusable Across Projects**
Once packaged, we can use it in all our projects. Plus, we're giving back to the C# game dev community - nobody else has made this wrapper yet!

### 4. **Version Control**
We can lock to specific versions of `msdf-atlas-gen` and update when we want, not when we remember.

---

## What is msdf-atlas-gen?

`msdf-atlas-gen` is a C++ command-line tool created by Viktor Chlumský that:
- Takes a font file (TTF/OTF)
- Generates **multi-channel signed distance fields** (MSDF) for each character
- Packs them into a texture atlas
- Outputs the atlas image + JSON with glyph metrics

**Why MSDF?** Regular bitmap fonts look blurry when scaled. MSDF fonts stay crisp at any size, perfect for games that need to support different resolutions.

We're NOT reimplementing this tool - we're just making it easy to call from C#.

---

## Project Structure (MVP)

```
Olve.MsdfAtlasGen/
├── src/
│   └── Olve.MsdfAtlasGen/
│       ├── Olve.MsdfAtlasGen.csproj
│       ├── AtlasGenerator.cs           # Main API - wraps the CLI tool
│       ├── Models/
│       │   ├── AtlasConfig.cs          # Input configuration
│       │   ├── AtlasResult.cs          # Output result with typed data
│       │   ├── AtlasType.cs            # Enum: SDF, MSDF, MTSDF
│       │   ├── GlyphInfo.cs            # Per-character data
│       │   ├── KerningPair.cs          # Letter spacing adjustments
│       │   └── FontMetrics.cs          # Line height, ascender, etc.
│       ├── Internal/
│       │   ├── NativeBinaryLocator.cs  # Finds the right binary for the OS
│       │   └── JsonModels.cs           # Raw JSON deserialization models
│       └── runtimes/                   # Native binaries (committed to repo)
│           ├── win-x64/
│           │   └── native/
│           │       └── msdf-atlas-gen.exe
│           └── linux-x64/
│               └── native/
│                   └── msdf-atlas-gen
├── .github/
│   └── workflows/
│       └── pack-and-publish.yml        # Packs NuGet and publishes
├── README.md                           # Basic usage documentation
├── LICENSE                             # MIT license
└── .gitignore
```

**Note:** The `runtimes/` folder binaries are pre-built and committed to the repository.

---

## Building Native Binaries

### The Approach

`msdf-atlas-gen` is a **native C++ application**. We'll:
1. Build binaries for Windows and Linux once
2. Commit them to the repository
3. At NuGet packaging time, just include the appropriate binaries

### Building Binaries (One-Time Setup)

You can build the binaries once manually or use CI to build them initially:

**Windows:**
```bash
git clone --recursive https://github.com/Chlumsky/msdf-atlas-gen
cd msdf-atlas-gen
cmake -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
# Binary will be in build/bin/Release/msdf-atlas-gen.exe
```

**Linux:**
```bash
sudo apt-get update
sudo apt-get install -y build-essential cmake
git clone --recursive https://github.com/Chlumsky/msdf-atlas-gen
cd msdf-atlas-gen
cmake -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
chmod +x build/bin/msdf-atlas-gen
# Binary will be in build/bin/msdf-atlas-gen
```

Once built, copy the binaries to:
- `src/Olve.MsdfAtlasGen/runtimes/win-x64/native/msdf-atlas-gen.exe`
- `src/Olve.MsdfAtlasGen/runtimes/linux-x64/native/msdf-atlas-gen`

Then commit them to the repository.

### GitHub Actions Workflow

A simple workflow to pack and publish:

```yaml
# .github/workflows/pack-and-publish.yml
name: Pack and Publish NuGet

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]
  release:
    types: [ created ]
  workflow_dispatch:

jobs:
  pack:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Pack NuGet package
        run: dotnet pack src/Olve.MsdfAtlasGen/Olve.MsdfAtlasGen.csproj -c Release

      - name: Push to NuGet (on release)
        if: github.event_name == 'release'
        run: |
          dotnet nuget push src/Olve.MsdfAtlasGen/bin/Release/Olve.MsdfAtlasGen.*.nupkg \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --source https://api.nuget.org/v3/index.json

      - name: Upload package artifact
        uses: actions/upload-artifact@v4
        with:
          name: nuget-package
          path: src/Olve.MsdfAtlasGen/bin/Release/*.nupkg
```

**Key Points:**
- Binaries are already in the repo, so we just pack them
- Much simpler than building in CI
- Only pushes to NuGet.org when you create a GitHub release

---

## How NuGet Runtime Packages Work

### The Magic of `runtimes/` Folder

When you structure your package like this:
```
runtimes/
├── win-x64/native/msdf-atlas-gen.exe
└── linux-x64/native/msdf-atlas-gen
```

NuGet automatically:
1. Detects the current OS/architecture at runtime
2. Copies the correct binary to the output directory
3. Makes it available to your code

**Your C# code then finds it:**
```csharp
public class NativeBinaryLocator
{
    public static string GetBinaryPath()
    {
        var rid = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64"
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux-x64"
                : throw new PlatformNotSupportedException("Only Windows and Linux are supported");
        
        var binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "msdf-atlas-gen.exe" 
            : "msdf-atlas-gen";
        
        var basePath = AppContext.BaseDirectory;
        var binaryPath = Path.Combine(basePath, "runtimes", rid, "native", binaryName);
        
        if (!File.Exists(binaryPath))
            throw new FileNotFoundException($"Native binary not found: {binaryPath}");
        
        // On Linux/Mac, ensure it's executable
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var chmod = Process.Start(new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x {binaryPath}",
                CreateNoWindow = true
            });
            chmod?.WaitForExit();
        }
        
        return binaryPath;
    }
}
```

---

## The .csproj Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    
    <!-- NuGet Package Metadata -->
    <PackageId>Olve.MsdfAtlasGen</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Company>Olve</Company>
    <Description>
      C# wrapper for msdf-atlas-gen with cross-platform native binaries included.
      Generates multi-channel signed distance field font atlases from TTF/OTF files.
    </Description>
    <PackageTags>msdf;sdf;font;atlas;distance-field;text-rendering;opengl;game-dev</PackageTags>
    <PackageProjectUrl>https://github.com/yourusername/olve-msdfatlasgen</PackageProjectUrl>
    <RepositoryUrl>https://github.com/yourusername/olve-msdfatlasgen</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <!-- Include README in package -->
  <ItemGroup>
    <None Include="../../README.md" Pack="true" PackagePath="/" />
  </ItemGroup>

  <!-- Include native binaries for all platforms -->
  <ItemGroup>
    <Content Include="runtimes/win-x64/native/msdf-atlas-gen.exe">
      <PackagePath>runtimes/win-x64/native/</PackagePath>
      <Pack>true</Pack>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
    <Content Include="runtimes/linux-x64/native/msdf-atlas-gen">
      <PackagePath>runtimes/linux-x64/native/</PackagePath>
      <Pack>true</Pack>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

  <!-- Dependencies -->
  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>
</Project>
```

**Note:** Since binaries are committed to the repo, no conditionals are needed - they're always present.

---

## Core Implementation Files

### 1. Models/AtlasConfig.cs
```csharp
namespace Olve.MsdfAtlasGen.Models
{
    public class AtlasConfig
    {
        public string FontPath { get; set; }
        public AtlasType Type { get; set; } = AtlasType.MSDF;
        public (int Width, int Height) Dimensions { get; set; } = (1024, 1024);
        public int GlyphSize { get; set; } = 48;
        public int PixelRange { get; set; } = 4;
        public string? CharsetFile { get; set; }
        public string? OutputImagePath { get; set; }
        public string? OutputJsonPath { get; set; }
    }
}
```

### 2. Models/AtlasType.cs
```csharp
namespace Olve.MsdfAtlasGen.Models
{
    public enum AtlasType
    {
        HardMask,
        SoftMask,
        SDF,
        PSDF,
        MSDF,
        MTSDF
    }
}
```

### 3. Models/AtlasResult.cs
```csharp
namespace Olve.MsdfAtlasGen.Models
{
    public class AtlasResult
    {
        public string ImagePath { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float DistanceRange { get; set; }
        public FontMetrics Metrics { get; set; }
        public List<GlyphInfo> Glyphs { get; set; }
        public List<KerningPair> Kerning { get; set; }
    }
}
```

### 4. Models/GlyphInfo.cs
```csharp
namespace Olve.MsdfAtlasGen.Models
{
    public class GlyphInfo
    {
        public char Character { get; set; }
        public int Unicode { get; set; }
        public float Advance { get; set; }
        public (float Left, float Bottom, float Right, float Top) PlaneBounds { get; set; }
        public (float Left, float Bottom, float Right, float Top) AtlasBounds { get; set; }
    }
}
```

### 5. Models/FontMetrics.cs
```csharp
namespace Olve.MsdfAtlasGen.Models
{
    public class FontMetrics
    {
        public float EmSize { get; set; }
        public float LineHeight { get; set; }
        public float Ascender { get; set; }
        public float Descender { get; set; }
        public float UnderlineY { get; set; }
        public float UnderlineThickness { get; set; }
    }
}
```

### 6. Models/KerningPair.cs
```csharp
namespace Olve.MsdfAtlasGen.Models
{
    public class KerningPair
    {
        public char First { get; set; }
        public char Second { get; set; }
        public float Advance { get; set; }
    }
}
```

### 7. Internal/JsonModels.cs
```csharp
using System.Text.Json.Serialization;

namespace Olve.MsdfAtlasGen.Internal
{
    internal class AtlasJsonData
    {
        [JsonPropertyName("atlas")]
        public AtlasInfo Atlas { get; set; }
        
        [JsonPropertyName("metrics")]
        public MetricsInfo Metrics { get; set; }
        
        [JsonPropertyName("glyphs")]
        public List<GlyphData> Glyphs { get; set; }
        
        [JsonPropertyName("kerning")]
        public List<KerningData> Kerning { get; set; }
    }

    internal class AtlasInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("distanceRange")]
        public float DistanceRange { get; set; }
        
        [JsonPropertyName("width")]
        public int Width { get; set; }
        
        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    internal class MetricsInfo
    {
        [JsonPropertyName("emSize")]
        public float EmSize { get; set; }
        
        [JsonPropertyName("lineHeight")]
        public float LineHeight { get; set; }
        
        [JsonPropertyName("ascender")]
        public float Ascender { get; set; }
        
        [JsonPropertyName("descender")]
        public float Descender { get; set; }
        
        [JsonPropertyName("underlineY")]
        public float UnderlineY { get; set; }
        
        [JsonPropertyName("underlineThickness")]
        public float UnderlineThickness { get; set; }
    }

    internal class GlyphData
    {
        [JsonPropertyName("unicode")]
        public int Unicode { get; set; }
        
        [JsonPropertyName("advance")]
        public float Advance { get; set; }
        
        [JsonPropertyName("planeBounds")]
        public BoundsData PlaneBounds { get; set; }
        
        [JsonPropertyName("atlasBounds")]
        public BoundsData AtlasBounds { get; set; }
    }

    internal class BoundsData
    {
        [JsonPropertyName("left")]
        public float Left { get; set; }
        
        [JsonPropertyName("bottom")]
        public float Bottom { get; set; }
        
        [JsonPropertyName("right")]
        public float Right { get; set; }
        
        [JsonPropertyName("top")]
        public float Top { get; set; }
    }

    internal class KerningData
    {
        [JsonPropertyName("unicode1")]
        public int Unicode1 { get; set; }
        
        [JsonPropertyName("unicode2")]
        public int Unicode2 { get; set; }
        
        [JsonPropertyName("advance")]
        public float Advance { get; set; }
    }
}
```

### 8. AtlasGenerator.cs (Main API)
```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Olve.MsdfAtlasGen.Internal;
using Olve.MsdfAtlasGen.Models;

namespace Olve.MsdfAtlasGen
{
    public class AtlasGenerator
    {
        public static AtlasResult Generate(AtlasConfig config)
        {
            if (string.IsNullOrEmpty(config.FontPath))
                throw new ArgumentException("FontPath is required", nameof(config));
            
            if (!File.Exists(config.FontPath))
                throw new FileNotFoundException($"Font file not found: {config.FontPath}");
            
            // Locate native binary
            var binaryPath = NativeBinaryLocator.GetBinaryPath();
            
            // Generate temp paths if not specified
            var outputImage = config.OutputImagePath ?? Path.Combine(Path.GetTempPath(), $"atlas_{Guid.NewGuid()}.png");
            var outputJson = config.OutputJsonPath ?? Path.Combine(Path.GetTempPath(), $"atlas_{Guid.NewGuid()}.json");
            
            // Build command-line arguments
            var args = BuildArguments(config, outputImage, outputJson);
            
            // Execute msdf-atlas-gen
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = binaryPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            if (process.ExitCode != 0)
            {
                throw new Exception($"msdf-atlas-gen failed with exit code {process.ExitCode}: {error}");
            }
            
            // Parse JSON output
            if (!File.Exists(outputJson))
            {
                throw new FileNotFoundException($"Output JSON not generated: {outputJson}");
            }
            
            var json = File.ReadAllText(outputJson);
            var data = JsonSerializer.Deserialize<AtlasJsonData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (data == null)
            {
                throw new Exception("Failed to parse atlas JSON data");
            }
            
            // Convert to result model
            return new AtlasResult
            {
                ImagePath = outputImage,
                Width = data.Atlas.Width,
                Height = data.Atlas.Height,
                DistanceRange = data.Atlas.DistanceRange,
                Metrics = new FontMetrics
                {
                    EmSize = data.Metrics.EmSize,
                    LineHeight = data.Metrics.LineHeight,
                    Ascender = data.Metrics.Ascender,
                    Descender = data.Metrics.Descender,
                    UnderlineY = data.Metrics.UnderlineY,
                    UnderlineThickness = data.Metrics.UnderlineThickness
                },
                Glyphs = data.Glyphs.Select(g => new GlyphInfo
                {
                    Character = (char)g.Unicode,
                    Unicode = g.Unicode,
                    Advance = g.Advance,
                    PlaneBounds = (g.PlaneBounds.Left, g.PlaneBounds.Bottom, g.PlaneBounds.Right, g.PlaneBounds.Top),
                    AtlasBounds = (g.AtlasBounds.Left, g.AtlasBounds.Bottom, g.AtlasBounds.Right, g.AtlasBounds.Top)
                }).ToList(),
                Kerning = data.Kerning.Select(k => new KerningPair
                {
                    First = (char)k.Unicode1,
                    Second = (char)k.Unicode2,
                    Advance = k.Advance
                }).ToList()
            };
        }
        
        private static string BuildArguments(AtlasConfig config, string outputImage, string outputJson)
        {
            var sb = new StringBuilder();
            
            sb.Append($"-font \"{config.FontPath}\" ");
            sb.Append($"-type {config.Type.ToString().ToLower()} ");
            sb.Append($"-dimensions {config.Dimensions.Width} {config.Dimensions.Height} ");
            sb.Append($"-size {config.GlyphSize} ");
            sb.Append($"-pxrange {config.PixelRange} ");
            sb.Append($"-format png ");
            sb.Append($"-imageout \"{outputImage}\" ");
            sb.Append($"-json \"{outputJson}\" ");
            
            if (!string.IsNullOrEmpty(config.CharsetFile))
            {
                sb.Append($"-charset \"{config.CharsetFile}\" ");
            }
            
            return sb.ToString();
        }
    }
}
```

---

## Implementation Steps

### Phase 1: Project Setup

1. **Create repository structure**
   ```bash
   mkdir Olve.MsdfAtlasGen
   cd Olve.MsdfAtlasGen
   git init
   
   mkdir -p src/Olve.MsdfAtlasGen
   mkdir -p .github/workflows
   ```

2. **Create .gitignore**
   ```
   bin/
   obj/
   *.user
   *.suo
   .vs/
   ```

3. **Create runtime folders and add binaries**
   ```bash
   mkdir -p src/Olve.MsdfAtlasGen/runtimes/win-x64/native
   mkdir -p src/Olve.MsdfAtlasGen/runtimes/linux-x64/native
   # Then build and copy the native binaries here (see "Building Native Binaries" section above)
   ```

4. **Build and commit native binaries**
   - Follow the "Building Native Binaries" section above to build binaries for Windows and Linux
   - Copy them to the respective `runtimes/` folders
   - Commit them to git

5. **Create the project**
   ```bash
   cd src/Olve.MsdfAtlasGen
   dotnet new classlib -n Olve.MsdfAtlasGen
   rm Class1.cs
   ```

6. **Set up project structure**
   ```bash
   mkdir Models
   mkdir Internal
   ```

### Phase 2: Implement Core Code

1. Implement all model classes (AtlasConfig, AtlasResult, GlyphInfo, etc.)
2. Implement Internal/JsonModels.cs for deserialization
3. Implement Internal/NativeBinaryLocator.cs
4. Implement AtlasGenerator.cs

### Phase 3: Set Up CI/CD

1. Create `.github/workflows/pack-and-publish.yml` (see workflow example above)
2. Add NuGet API key to GitHub secrets:
    - Go to repository Settings → Secrets and variables → Actions
    - Add secret named `NUGET_API_KEY` with your NuGet.org API key

### Phase 4: Create Documentation

1. **README.md**
   ```markdown
   # Olve.MsdfAtlasGen

   C# wrapper for [msdf-atlas-gen](https://github.com/Chlumsky/msdf-atlas-gen) with cross-platform native binaries included.

   ## Installation

   ```bash
   dotnet add package Olve.MsdfAtlasGen
   ```

   ## Usage

   ```csharp
   using Olve.MsdfAtlasGen;

   var result = AtlasGenerator.Generate(new AtlasConfig
   {
       FontPath = "Roboto-Regular.ttf",
       Type = AtlasType.MSDF,
       Dimensions = (1024, 1024),
       GlyphSize = 48,
       PixelRange = 4,
       OutputImagePath = "atlas.png",
       OutputJsonPath = "atlas.json"
   });

   Console.WriteLine($"Generated {result.Glyphs.Count} glyphs");
   Console.WriteLine($"Line height: {result.Metrics.LineHeight}");
   ```

   ## Credits

   This is a wrapper around Viktor Chlumský's excellent [msdf-atlas-gen](https://github.com/Chlumsky/msdf-atlas-gen) tool.
   All atlas generation is performed by the native msdf-atlas-gen binary.

   ## License

   MIT License. See LICENSE file.
   The msdf-atlas-gen tool is licensed under its own terms - see the [upstream repository](https://github.com/Chlumsky/msdf-atlas-gen).
   ```

2. **LICENSE**
   ```
   MIT License

   Copyright (c) 2025 [Your Name]

   Permission is hereby granted, free of charge, to any person obtaining a copy
   of this software and associated documentation files (the "Software"), to deal
   in the Software without restriction, including without limitation the rights
   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
   copies of the Software, and to permit persons to whom the Software is
   furnished to do so, subject to the following conditions:

   The above copyright notice and this permission notice shall be included in all
   copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
   SOFTWARE.
   ```

### Phase 5: Testing & Release

1. **Test locally** - Since binaries are committed, you can test immediately
2. **Push to GitHub** - This triggers the pack workflow
3. **Verify package** - Check GitHub Actions artifacts
4. **Create a GitHub release** to trigger NuGet publish:
   - Go to GitHub Releases → Create new release
   - Tag version (e.g., `v1.0.0`)
   - This will automatically publish to NuGet.org
5. **Verify on NuGet.org** - Package should appear within minutes

---

## Local Development

Since binaries are committed to the repository:

1. **Everything works locally** - No CI needed for development
2. **Test on your platform** - Windows or Linux, both binaries are available
3. **Cross-platform testing** requires access to both OSes (VM, WSL, etc.)

---

## Common Issues & Solutions

### Issue: "Native binary not found"
**Cause:** Binary not in correct location or not marked for copy  
**Fix:** Verify `CopyToOutputDirectory="PreserveNewest"` in .csproj and binary exists

### Issue: "Permission denied" on Linux/Mac
**Cause:** Binary not executable  
**Fix:** `NativeBinaryLocator` includes chmod call (see implementation above)

### Issue: CI build fails on submodule checkout
**Cause:** msdf-atlas-gen has submodules  
**Fix:** Ensure `submodules: recursive` in checkout action

### Issue: Different command-line syntax across platforms
**Cause:** Path separators or quote escaping  
**Fix:** Always use `Path.Combine()` and quote paths in arguments

---

## Success Criteria

MVP is complete when:
1. ✅ Native binaries for Windows and Linux are built and committed to repo
2. ✅ GitHub Actions successfully packs the NuGet package
3. ✅ Package can be installed via `dotnet add package Olve.MsdfAtlasGen`
4. ✅ Basic usage code (from README) works on Windows and Linux
5. ✅ Generated atlas.png and atlas.json are valid
