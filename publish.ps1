$runtimeIDs = "win-x64","win-x86"

foreach ($runtime in $runtimeIDs)
{
	$pubdir="./publish/$runtime"
	
	dotnet publish OverdriveDownloader/OverdriveDownloader.csproj `
		-r $runtime `
		--self-contained  `
		-p:PublishSingleFile=true `
		-p:PublishReadyToRun=true `
		-p:PublishTrimmed=true `
		-c Release `
		-o $pubdir
		
	rm "$pubdir/*.so"
	rm "$pubdir/*.dll"
	rm "$pubdir/*.dylib"
	
	Compress-Archive $pubdir "./publish/OverdriveDownloader-$runtime.zip"
	rm -r $pubdir
}
