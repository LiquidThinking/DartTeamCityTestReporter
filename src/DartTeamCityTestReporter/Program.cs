using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DartTeamCityTestReporter
{
	public class Program
	{
		private static bool _seenFirstGroup;
		private static readonly Dictionary<int, string> _testNames = new Dictionary<int, string>();
		private static readonly List<Suite> _suites = new List<Suite>();

		public static void Main( string[] args )
		{
			var parsedArguments = new ArgumentsParser().Parse( args );

			var workingDirectory = parsedArguments.ContainsKey( "wd" ) ? parsedArguments[ "wd" ] : "";
			var command = parsedArguments[ "" ];

			var processStartInfo = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = "/C " + command,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				WorkingDirectory = workingDirectory,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			using ( var process = Process.Start( processStartInfo ) )
			{
				process.OutputDataReceived += ( sender, eventArgs ) => {
												if ( eventArgs.Data != null && eventArgs.Data.Contains( "was not terminated" ) )
													process.Kill();
												ParseOutput( eventArgs.Data );
											};
				process.BeginOutputReadLine();
				process.WaitForExit();
			}
			Thread.Sleep( 1000 );
		}

		private static void ParseOutput( string line )
		{
			if ( line == null || !line.StartsWith( "{" ) )
				return;

			if ( !_seenFirstGroup && !line.StartsWith( "{\"group\"" ) && !line.Contains( "\"name\":null" ) )
				return;
			_seenFirstGroup = true;

			if ( line.StartsWith( "{\"group\"" ) )
				ParseGroup( line );
			else if ( line.StartsWith( "{\"test\"" ) )
				ParseTest( line );
			else if ( line.StartsWith( "{\"testID\"" ) )
				ParseTestId( line );
		}

		private static void ParseTestId( string line )
		{
			var test = JsonConvert.DeserializeObject<TestId>( line );
			if ( !_testNames.ContainsKey( test.testID ) )
				return;
			var name = _testNames[ test.testID ];

			var isFinished = !String.IsNullOrEmpty( test.error ) || test.result != null;
			if ( isFinished )
				_testNames.Remove( test.testID );

			if ( !string.IsNullOrEmpty( test.error ) )
				Console.Write( test.error );

			if ( test.messageType == "print" )
			{
				Console.WriteLine( test.message );
				return;
			}


			if ( test.result != "success" )
				Console.WriteLine( $"##teamcity[testFailed name='{EscapeName( name )}']" );
			Console.WriteLine( $"##teamcity[testFinished name='{EscapeName( name )}' duration='{test.time}']" );

			_suites.ForEach( x => x.TestsRemaining-- );
			foreach ( var suite in _suites.Where( x => x.TestsRemaining == 0 ).OrderByDescending( x => x.Order ).ToList() )
			{
				_suites.Remove( suite );
				Console.WriteLine( $"##teamcity[testSuiteFinished name='{EscapeName( suite.Name )}']" );
			}
		}

		private static string EscapeName( string name ) => name.Replace( "'", "|'" );

		private static void ParseTest( string line )
		{
			var testLine = JsonConvert.DeserializeObject<TestLine>( line );
			_testNames.Add( testLine.test.id, testLine.test.name );
			Console.WriteLine( $"##teamcity[testStarted name='{EscapeName( testLine.test.name )}' captureStandardOutput='true']" );
		}

		private static void ParseGroup( string line )
		{
			var groupLine = JsonConvert.DeserializeObject<GroupLine>( line );
			var groupName = groupLine.group.name ?? "tests.dart";

			Console.WriteLine( $"##teamcity[testSuiteStarted name='{EscapeName( groupName )}']" );

			_suites.Add( new Suite
			{
				GroupId = groupLine.group.id,
				Name = groupName,
				TestsRemaining = groupLine.group.testCount
			} );
		}
	}
}