@if exist "%ProgramFiles%" set MsBuildDir=%ProgramFiles%\MSBuild\14.0\Bin
@if exist "%ProgramFiles(x86)%" set MsBuildDir=%ProgramFiles(x86)%\MSBuild\14.0\Bin

"%MsBuildDir%\msbuild.exe" /nologo /t:Rebuild /p:Configuration=Debug /verbosity:m Synthesis.csproj
