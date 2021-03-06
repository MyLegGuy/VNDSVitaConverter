﻿/*
 * User: good
 * Date: 2/20/2018
 * Time: 7:10 PM
 */
using System;
using System.IO;
using System.Drawing;
// For specific image formats
using System.Drawing.Imaging;
using VNDSConverter;
using System.IO.Compression;
using System.Windows.Forms;
using System.Diagnostics;

namespace VNDSConverter{
	class Program{
		static string changeDirectoryPath(string _newDirectory){
			string _returnString = _newDirectory.TrimEnd(Path.DirectorySeparatorChar);
			string _normalFolderName = Path.GetFileName(_returnString);
			_returnString = Path.GetDirectoryName(_returnString);
			_returnString = _returnString + Path.DirectorySeparatorChar +_normalFolderName+"-converted";
			return _returnString;
		}
		// Path the root directory of the new folder
		static string getNewCGDirectoryA(string _rootDirectory){
			return Path.Combine(_rootDirectory,"CG"+Path.DirectorySeparatorChar);
		}
		static string getNewCGDirectoryB(string _rootDirectory){
			return Path.Combine(_rootDirectory,"CGAlt"+Path.DirectorySeparatorChar);
		}
		static string getNewScriptDirectory(string _rootDirectory){
			return Path.Combine(_rootDirectory,"Scripts"+Path.DirectorySeparatorChar);
		}
		static string getNewAudioDirectory(string _rootDirectory){
			return Path.Combine(_rootDirectory,"SE"+Path.DirectorySeparatorChar);
		}
		static string getNewAudioArchive(string _rootDirectory){
			return Path.Combine(_rootDirectory,"SEArchive.legArchive");
		}
		
		// Pass the root directory of the old folder

		// background
		static string getOldCGDirectoryA(string _rootDirectory){
			return Path.Combine(_rootDirectory,"background"+Path.DirectorySeparatorChar);
		}
		// characters
		static string getOldCGDirectoryB(string _rootDirectory){
			return Path.Combine(_rootDirectory,"foreground"+Path.DirectorySeparatorChar);
		}
		static string getOldScriptDirectory(string _rootDirectory){
			return Path.Combine(_rootDirectory,"script"+Path.DirectorySeparatorChar);
		}
		static string getOldAudioDirectory(string _rootDirectory){
			return Path.Combine(_rootDirectory,"sound"+Path.DirectorySeparatorChar);
		}
		
		static void createNewDirectories(string _newRootDirectory){
			Directory.CreateDirectory(_newRootDirectory);
			Directory.CreateDirectory(getNewAudioDirectory(_newRootDirectory));
			Directory.CreateDirectory(getNewCGDirectoryA(_newRootDirectory));
			Directory.CreateDirectory(getNewCGDirectoryB(_newRootDirectory));
			Directory.CreateDirectory(getNewScriptDirectory(_newRootDirectory));
		}
		
		static void copyIfExist(string _sourceFile, string _destFile){
			if (File.Exists(_sourceFile)){
				if (Options.detailedConsoleOutput){
					Console.Out.WriteLine("[COPY] {0} to {1}",_sourceFile,_destFile);
				}
				File.Copy(_sourceFile,_destFile);
			}
		}
		
		// Also resizes image
		static void fixBitmap(ref Bitmap _toFix, bool _isBust){
			Bitmap _newImage;
			if (_isBust && Options.doImageRounding){
				_newImage = new Bitmap((int)Math.Ceiling(_toFix.Width/(double)Options.imageRoundUpWidth)*Options.imageRoundUpWidth,(int)Math.Ceiling(_toFix.Height/(double)Options.imageRoundUpHeight)*Options.imageRoundUpHeight,PixelFormat.Format32bppArgb);
				// Because some of the image won't be used.
				_newImage.MakeTransparent();
			}else{
				_newImage = new Bitmap((int)(_toFix.Width*Options.resizeRatio),(int)(_toFix.Height*Options.resizeRatio),PixelFormat.Format32bppArgb);
			}
			//
			if (Options.resizeRatio!=1.0 && Options.doImageRounding==false){
				using (Graphics g = Graphics.FromImage(_newImage)){
					g.DrawImage(_toFix,0,0,_newImage.Width,_newImage.Height);
				}
			}else{
				using (Graphics g = Graphics.FromImage(_newImage)){
					g.DrawImage(_toFix,0,0,_toFix.Width,_toFix.Height);
				}
			}
			_toFix.Dispose();
			_toFix = _newImage;
		}
		
		static void processBackgroundImages(string _sourceFile, string _destFile){
			_processSingleImage(_sourceFile,_destFile,false);
		}
		static void processCharacterImages(string _sourceFile, string _destFile){
			_processSingleImage(_sourceFile,_destFile,true);
		}
		static void copyAndOverwriteFile(string _inFile, string _outFile){
			File.Copy(_inFile,_outFile,true);
		}
		static void _convertSingleSound(string _inFile){
			if (Path.GetExtension(_inFile)==".aac"){
				if (Options.canUseFFmpeg){
					if (Options.detailedConsoleOutput){
						Console.Out.WriteLine("Process {0}",_inFile);
					}
					Process _FFmpegProcess = new Process();
					if (StolenCode.IsRunningOnMono()){
						_FFmpegProcess.StartInfo.FileName = "ffmpeg";
					}else{
						_FFmpegProcess.StartInfo.FileName = "ffmpeg.exe";
					}
					_FFmpegProcess.StartInfo.Arguments = "-i \""+_inFile+"\" \""+Path.ChangeExtension(_inFile,".ogg")+"\"";
					_FFmpegProcess.StartInfo.UseShellExecute = false;
					_FFmpegProcess.StartInfo.RedirectStandardOutput = true;
					_FFmpegProcess.Start();
					if (!Options.canInfiniteProcess){
						// Don't want my users' computers to explode
						_FFmpegProcess.WaitForExit(3000);
					}
				}else{
					if (Options.errorConsoleOutput){ // Because this is important
						Console.Out.WriteLine("Skip .aac file {0}",_inFile);
					}
				}
			}
		}
		// based on https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
		// Function that should be used to copy all files in a directory and its subdirectories. The function you pass it is called with two filenames, the source and destination filename. You can process the source file and output it to destination filename.
		static void processDirectory(string _sourceDirectory, string _destDirectory, Action<string, string> _everyFileFunction){
			if (Options.simpleConsoleOutput){
				Console.Out.Write("Process {0} to {1}\n",_sourceDirectory,_destDirectory);
			}
			// Get the subdirectories for the specified directory.
			DirectoryInfo _currentDirectoryInfo = new DirectoryInfo(_sourceDirectory);
			if (!_currentDirectoryInfo.Exists){
				throw new DirectoryNotFoundException("Source directory does not exist or could not be found: "+ _sourceDirectory);
			}
			// If the destination destination directory doesn't exist, create it.
			if (!Directory.Exists(_destDirectory)){
				Directory.CreateDirectory(_destDirectory);
			}
			// Get the files in the directory and copy them to the new location. Does not include subdirectory files
			FileInfo[] _currentDirectoryFiles = _currentDirectoryInfo.GetFiles();
			for (int i=0;i<_currentDirectoryFiles.Length;++i){
				string temppath = Path.Combine(_destDirectory, _currentDirectoryFiles[i].Name/*.ToUpper()*/);
				_everyFileFunction(_currentDirectoryFiles[i].FullName,temppath);
			}
			DirectoryInfo[] _foundSubdirectories = _currentDirectoryInfo.GetDirectories();
			// Do this same function for all subdirectories
			for (int i=0;i<_foundSubdirectories.Length;++i){
				string temppath = Path.Combine(_destDirectory, _foundSubdirectories[i].Name/*.ToUpper()*/);
				processDirectory(_foundSubdirectories[i].FullName, temppath, _everyFileFunction);
			}
		}
		
		static void maybeExtractZIPFile(string _zipPath, string _destPath){
			if (File.Exists(_zipPath)){
				using (ZipArchive myZipArchive = ZipFile.OpenRead(_zipPath)){
					if (Options.simpleConsoleOutput){
						Console.Out.WriteLine("[EXTRACT] {0} to {1}",_zipPath,_destPath);
					}
					try{
						myZipArchive.ExtractToDirectory(_destPath,true);
					}catch(Exception e){
						Console.Out.WriteLine("Error extracting ZIP file.");
						Console.Out.WriteLine(e.ToString());
						if (StolenCode.IsRunningOnMono()){
							Console.Out.WriteLine("=============");
							Console.Out.WriteLine("Please make sure Mono is updated if it's not already.");
						}
						printPressAnyKey();
						Console.ReadKey();
						Environment.Exit(1);
					}
				}
				
			}else{
				if (Options.simpleConsoleOutput){
					Console.Out.WriteLine("[NOT EXTRACT] {0} not exist.",_zipPath);
				}
			}
		}
		
		static void _processSingleImage(string _sourceFile, string _destFile, bool _isBust){
			string _cachedExtension = Path.GetExtension(_sourceFile);
			if (_cachedExtension==".png" || _cachedExtension==".jpg" || _cachedExtension==".jpeg" || _cachedExtension==".bmp"){
				if (Options.detailedConsoleOutput){
					Console.Out.WriteLine("[Image] {0}",_sourceFile);
				}
				Bitmap _tempLoadedBitmap;
				try{
					_tempLoadedBitmap = new Bitmap(_sourceFile);
					//if (_tempLoadedBitmap.Width>_recordWidth){
					//	_recordWidth = _tempLoadedBitmap.Width;
					//}
					//if (_tempLoadedBitmap.Height>_recordHeight){
					//	_recordHeight = _tempLoadedBitmap.Height;
					//}
					fixBitmap(ref _tempLoadedBitmap,_isBust);
				}catch(Exception){
					if (Options.importantConsoleOutput){
						Console.Out.WriteLine("[BLACK] Force black image {0}",_sourceFile);
					}
					_tempLoadedBitmap = new Bitmap(256,192,PixelFormat.Format32bppArgb);
					using (Graphics g = Graphics.FromImage(_tempLoadedBitmap)){
						g.FillRectangle(Brushes.Black,0,0,_tempLoadedBitmap.Width,_tempLoadedBitmap.Height);
					}
					fixBitmap(ref _tempLoadedBitmap,_isBust);
				}
				_tempLoadedBitmap.Save(_destFile,ImageFormat.Png);
				/*if (_cachedExtension==".png"){
					_tempLoadedBitmap.Save(Path.Combine(_destDirectory,Path.GetFileName(_filesToProcess[i]).ToUpper()),ImageFormat.Png);
				}else if (_cachedExtension==".jpg" || _cachedExtension==".jpeg"){
					_tempLoadedBitmap.Save(Path.Combine(_destDirectory,Path.GetFileName(_filesToProcess[i]).ToUpper()),ImageFormat.Jpeg);
				}*/
				_tempLoadedBitmap.Dispose();
			}else{
				if (Options.errorConsoleOutput){
					Console.Out.WriteLine("[Skip] Non-image extension {0}.",Path.GetExtension(_sourceFile));
				}
			}
		}

		static void makeSoundArchive(string _sourceDirectory, string _destFile){
			if (Directory.Exists(_sourceDirectory)){
				// Step 1 - Convert any .aac audio into .ogg
				if (Options.simpleConsoleOutput){
					Console.Out.WriteLine("Converting aac audio if present");
				}
				string[] _foundFiles = Directory.GetFiles(_sourceDirectory,"*",SearchOption.AllDirectories);
				for (int i=0;i<_foundFiles.Length;++i){
					_convertSingleSound(_foundFiles[i]);
				}
	
				// Step 2 - Make archive
				_foundFiles = Directory.GetFiles(_sourceDirectory,"*",SearchOption.AllDirectories);
				LegArchive _soundArchive = new LegArchive(_destFile,_sourceDirectory);
				if (Options.simpleConsoleOutput){
					Console.Out.WriteLine("Creating sound archive");
				}
				for (int i=0;i<_foundFiles.Length;++i){
					if (Path.GetExtension(_foundFiles[i])!=".aac"){
						_soundArchive.addFile(_foundFiles[i]);
					}
				}
				if (Options.simpleConsoleOutput){
					Console.Out.WriteLine("Writing filename table");
				}
				_soundArchive.finish();
			}else{
				if (Options.simpleConsoleOutput){
					Console.Out.WriteLine("{0} not exist, so skipping sound",_sourceDirectory);
				}
			}
		}

		public static string doFunctionality(string _originalGameFolderName){
			// These variables will not be 0 if background images are being resized from their original img.ini value. These values should be written to a new img.ini.
			int _resizedBackgroundWidth=0;
			int _resizedBackgroundHeight=0; // As C# is for babies, I am forced to assign a value here even though I won't need it.
			if (File.Exists(Path.Combine(_originalGameFolderName,"img.ini")) && Options.resizeImages){
				StreamReader _tempImgIni = new StreamReader(new FileStream(Path.Combine(_originalGameFolderName,"img.ini"),FileMode.Open));
				string _readWidthLine = _tempImgIni.ReadLine(); // width=xxx
				string _readHeightLine = _tempImgIni.ReadLine();// height=xxx
				_tempImgIni.Dispose();
	
				try{
					int _readWidthNumber = Int32.Parse(_readWidthLine.Substring("width=".Length));
					int _readHeightNumber = Int32.Parse(_readHeightLine.Substring("height=".Length));
					
					double _fitToWidthRatio = Options.targetWidth/(double)_readWidthNumber;
					double _fitToHeightRatio = Options.targetHeight/(double)_readHeightNumber;
					Options.resizeRatio = _fitToWidthRatio<_fitToHeightRatio ? _fitToWidthRatio : _fitToHeightRatio;

					_resizedBackgroundWidth = (int)(_readWidthNumber*Options.resizeRatio);
					_resizedBackgroundHeight = (int)(_readHeightNumber*Options.resizeRatio);
				}catch(Exception e){
					if (Options.errorConsoleOutput){
						Console.Out.WriteLine(e.ToString());
						Console.Out.Write("Failed to load img.ini, abort.\n");
						return null;
					}
				}
			}else{
				Console.Out.WriteLine("Could not find img.ini, assuming DS resolution.");
			}

			string _newGameFolderPath = changeDirectoryPath(_originalGameFolderName);
			if (Options.autoDelete){
				if (Directory.Exists(_newGameFolderPath)){
					try{
						if (Options.importantConsoleOutput){
							Console.Out.WriteLine("Attempting to delete old conversion directory...");
						}
						Directory.Delete(_newGameFolderPath,true);
					}catch(Exception e){
						if (Options.errorConsoleOutput){
							Console.Out.WriteLine("{0}\nFailed to delete the old conversion directory. This probably means the program can't write to at least one of the files in the directory, so conversion will probably fail when it tries to overwrite that file.",e.ToString());
						}
					}
				}
			}
			createNewDirectories(_newGameFolderPath);
			
			maybeExtractZIPFile(Path.Combine(_originalGameFolderName,"background.zip"),_originalGameFolderName);
			maybeExtractZIPFile(Path.Combine(_originalGameFolderName,"foreground.zip"),_originalGameFolderName);
			maybeExtractZIPFile(Path.Combine(_originalGameFolderName,"script.zip"),_originalGameFolderName);
			maybeExtractZIPFile(Path.Combine(_originalGameFolderName,"sound.zip"),_originalGameFolderName);
			
			makeSoundArchive(getOldAudioDirectory(_originalGameFolderName),getNewAudioArchive(_newGameFolderPath));
			processDirectory(getOldScriptDirectory(_originalGameFolderName),getNewScriptDirectory(_newGameFolderPath),copyAndOverwriteFile);
			processDirectory(getOldCGDirectoryA(_originalGameFolderName),getNewCGDirectoryA(_newGameFolderPath),processBackgroundImages);
			processDirectory(getOldCGDirectoryB(_originalGameFolderName),getNewCGDirectoryB(_newGameFolderPath),processCharacterImages);
			if (Options.simpleConsoleOutput){
				Console.Out.WriteLine("[COPY] Assorted root game directory files");
			}
			copyIfExist(Path.Combine(_originalGameFolderName,"default.ttf"),Path.Combine(_newGameFolderPath,"default.ttf"));
			copyIfExist(Path.Combine(_originalGameFolderName,"info.txt"),Path.Combine(_newGameFolderPath,"info.txt"));
			if (_resizedBackgroundWidth==0){
				copyIfExist(Path.Combine(_originalGameFolderName,"img.ini"),Path.Combine(_newGameFolderPath,"img.ini"));
			}else{
				if (Options.simpleConsoleOutput){
					Console.Out.WriteLine("Make new img.ini.");
				}
				// If our backgrounds have been resized
				StreamWriter _newImgIni = new StreamWriter(new FileStream(Path.Combine(_newGameFolderPath,"img.ini"),FileMode.Create));
				_newImgIni.WriteLine("width="+_resizedBackgroundWidth);
				_newImgIni.WriteLine("height="+_resizedBackgroundHeight);
				_newImgIni.Dispose();
			}
			if (File.Exists(Path.Combine(_originalGameFolderName,"icon.png"))){
				Console.Out.WriteLine("Fix and copy icon.png");
				_processSingleImage(Path.Combine(_originalGameFolderName,"icon.png"),Path.Combine(_newGameFolderPath,"icon.png"),false);
			}
			if (File.Exists(Path.Combine(_originalGameFolderName,"thumbnail.png"))){
				Console.Out.WriteLine("Fix and copy thumbnail.png");
				_processSingleImage(Path.Combine(_originalGameFolderName,"thumbnail.png"),Path.Combine(_newGameFolderPath,"thumbnail.png"),false);
			}
			
			
			if (Options.simpleConsoleOutput){
				Console.Out.WriteLine("[CREATE] {0}",Path.Combine(_newGameFolderPath,"isvnds"));
			}
			File.WriteAllText(Path.Combine(_newGameFolderPath,"isvnds"),Options.platformName);
			
			BinaryWriter bw = new BinaryWriter(new FileStream(Path.Combine(_newGameFolderPath,"vndsvitaproperties"),FileMode.Create));
			bw.Write(Options.writtenVersionNumber);
			bw.Dispose();
			
			return _newGameFolderPath;
		}
		
		static void toggleDependingOnArgs(string[] args, ref int i, ref bool _toToggle){
			++i;
			if (args[i]!="off"){
				_toToggle=true;
			}else{
				_toToggle=false;
			}
		}
		
		static void printPressAnyKey(){
			Console.Write("Press any key to continue . . .\n");
		}
		
		static bool getFFmpegExist(){
			Process _possibleFFmpegProcess = new Process();
			if (StolenCode.IsRunningOnMono()){
				_possibleFFmpegProcess.StartInfo.FileName = "ffmpeg";
			}else{
				_possibleFFmpegProcess.StartInfo.FileName = "ffmpeg.exe";
			}
			_possibleFFmpegProcess.StartInfo.Arguments = "-version";
			_possibleFFmpegProcess.StartInfo.UseShellExecute = false;
			_possibleFFmpegProcess.StartInfo.RedirectStandardOutput = true;
			try{
				_possibleFFmpegProcess.Start();
				_possibleFFmpegProcess.WaitForExit();
			}catch(Exception){ // Windows throws error if file not found
				return false;
			}
			if (_possibleFFmpegProcess.StandardOutput.ReadToEnd().StartsWith("ffmpeg version")){
				return true;
			}else{
				return false;
			}
		}

		[STAThread]
		static void Main(string[] args){
			string _sourceFile=null;
			Console.Out.Write("Checking for FFmpeg...");
			Options.canUseFFmpeg = getFFmpegExist();
			Console.Out.WriteLine(Options.canUseFFmpeg);

			Console.Out.WriteLine("Write v"+Options.writtenVersionNumber);
			if (args.Length==0){
				if (!StolenCode.IsRunningOnMono()){
					Application.EnableVisualStyles();
					Application.SetCompatibleTextRenderingDefault(false);
					MainForm myMainForm = new MainForm();
					Application.Run(myMainForm);
					_sourceFile = myMainForm.confirmedChosenDirectory;
					if (_sourceFile==null){
						Console.Out.WriteLine("_sourceFile==null");
						return;
					}
				}else{
					args = new string[1];
					args[0]=null;

					if (!Options.canUseFFmpeg){
						Console.Out.WriteLine("\n\nI think FFmpeg is not installed. AAC audio won't be able to be converted and played on the Vita. If you want this feature, please install FFmpeg and make sure it's in PATH. Run from terminal and append -forceffmpeg to force FFmpeg usage even if it's not found. \n\n");
					}

					while (args[0]==null){
						Console.Out.WriteLine("Enter the VNDS game's folder path. The path should end in a slash and the folder should be in a writable directory\nFor example, /home/nathan/higurashi/\n");
						args[0] = Console.ReadLine();
						if (!Directory.Exists(args[0])){
							Console.Out.WriteLine("\nDirectory {0} does not exist.",args[0]);
							args[0]=null;
						}
					}

					// Platform prompt
					do{
						Console.Out.WriteLine("\n\nEnter the number of the platform you want to convert for.\n");
						for (int j=0;j<Options.possiblePlatforms.Length;++j){
							Console.Out.WriteLine("{0}) {1}",j,Options.possiblePlatforms[j]);
						}

						int _userSelection;
						if(Int32.TryParse(Console.ReadLine(), out _userSelection)){
							Options.applyPlatformPresent(Options.possiblePlatforms[_userSelection]);
						}else{
							Console.Out.WriteLine("User input was not a nu- wait a second, this isn't in Windows mode. I expected more from you.");
						}
					}while(Options.platformName==null);
				}
			}
			int i;
			for (i=0;i<args.Length;i++){
				if (args[i][0]!='-'){
					_sourceFile = args[i];
				}else{
					if (args[i]=="-simpleoutput"){
						toggleDependingOnArgs(args,ref i, ref Options.simpleConsoleOutput);
					}else if (args[i]=="-detailedoutput"){
						toggleDependingOnArgs(args,ref i, ref Options.detailedConsoleOutput);
					}else if (args[i]=="-erroroutput"){
						toggleDependingOnArgs(args,ref i, ref Options.errorConsoleOutput);
					}else if (args[i]=="-importantoutput"){
						toggleDependingOnArgs(args,ref i, ref Options.importantConsoleOutput);
					}else if (args[i]=="-autodelete"){
						toggleDependingOnArgs(args,ref i, ref Options.autoDelete);
					}else if (args[i]=="-ffmpeg"){
						toggleDependingOnArgs(args,ref i, ref Options.canUseFFmpeg);
					}else if (args[i]=="-forceffmpeg"){
						Options.canUseFFmpeg=true;
					}else if (args[i]=="-infiniteprocesses"){
						Options.canInfiniteProcess=true;
					}
				}
			}
			if (_sourceFile==null){
				if (Options.importantConsoleOutput){
					Console.Out.WriteLine("No path found. Make sure your path doesn't start with a hyphen.");
					printPressAnyKey();
				}
				Console.ReadKey();
				return;
			}
			if (!File.Exists(Path.Combine(_sourceFile,"info.txt"))){
				Console.Out.WriteLine("{0} does not exist, so I assume that this IS NOT a VNDS game folder. Retry.\n",Path.Combine(_sourceFile,"info.txt"));
				return;
			}

			//for (i=0;i<possibleFreacLocations.Length;++i){
			//	if (File.Exists(possibleFreacLocations[i])){
			//		Options.canConvertAudio=true;
			//		Options.actualFreacLocation = possibleFreacLocations[i];
			//		break;
			//	}
			//}
			//if (!Options.canConvertAudio){
			//	Console.out.WriteLine("This is the version without audio conversion. DS novels may not convert correctly.");
			//}

			if (Options.importantConsoleOutput){
				Console.WriteLine("Hello World!");
			}
			// TODO: Implement Functionality Here
			if (Options.importantConsoleOutput){
				Console.Out.WriteLine("Done, you may close this window.\nThe converted game is at {0}",doFunctionality(_sourceFile));
				printPressAnyKey();
				Console.ReadKey(true);
			}else{
				doFunctionality(_sourceFile);
			}
		}
	}
}
