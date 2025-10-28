# Native Binaries

This directory should contain the pre-built native binaries for `msdf-atlas-gen`.

## Required Structure

```
runtimes/
├── win-x64/
│   └── native/
│       └── msdf-atlas-gen.exe
└── linux-x64/
    └── native/
        └── msdf-atlas-gen
```

## Building the Native Binaries

Follow the instructions in TASK.md to build the native binaries for each platform:

### Windows (on Windows or using cross-compilation):
```bash
git clone --recursive https://github.com/Chlumsky/msdf-atlas-gen
cd msdf-atlas-gen
cmake -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
# Binary will be in build/bin/Release/msdf-atlas-gen.exe
```

### Linux (on Linux or using WSL/Docker):
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

## After Building

1. Copy the Windows binary to `runtimes/win-x64/native/msdf-atlas-gen.exe`
2. Copy the Linux binary to `runtimes/linux-x64/native/msdf-atlas-gen`
3. Commit the binaries to the repository
4. Build and pack the NuGet package

The binaries will be automatically included in the NuGet package when they are present in these directories.
