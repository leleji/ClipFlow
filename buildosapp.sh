dotnet restore -r osx-arm64
dotnet msbuild -t:BundleApp -p:RuntimeIdentifier=osx-arm64  -property:Configuration=Release