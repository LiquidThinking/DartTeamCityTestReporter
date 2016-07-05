using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DartTeamCityTestReporter
{
    public class Suite
    {
        private static int _order = 1;

        public Suite()
        {
            Order = _order++;
        }

        public int Order { get; set; }
        public int GroupId { get; set; }
        public string Name { get; set; }
        public int TestsRemaining { get; set; }
    }

    public class Program
    {
        private static bool _seenFirstGroup;
        private static readonly Dictionary<int,string> _testNames = new Dictionary<int, string>();
        private static readonly List<Suite> _suites = new List<Suite>();

        public static void Main(string[] args)
        {
            var parsedArguments = new ArgumentsParser().Parse( args );

            var testFile = parsedArguments[ "" ];

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dart",
                Arguments = $@"--ignore-unrecognized-flags --checked --trace_service_pause_events ""file:\\\C:\Program Files\Dart\dart-sdk\bin\snapshots\pub.dart.snapshot"" run test:test -r json -p dartium {testFile}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = @"D:\GitHub\LiveScoring\Client\LiveScoring"
            };
            using ( var process = Process.Start( processStartInfo ) )
            {
                while ( !process.HasExited )
                {
                    ParseOutput( process.StandardOutput.ReadLine() );
                }
            }
        }

        private static void ParseOutput( string line )
        {
            if ( line == null )
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
            var name = _testNames[ test.testID ];
            _testNames.Remove( test.testID );

            if ( test.result != "success" )
                Console.WriteLine( $"##teamcity[testFailed name='{name}']" );
            Console.WriteLine( $"##teamcity[testFinished name='{name}' duration='{test.time}']" );

            _suites.ForEach( x => x.TestsRemaining-- );
            foreach ( var suite in _suites.Where( x => x.TestsRemaining == 0 ).OrderByDescending( x => x.Order ).ToList() )
            {
                _suites.Remove( suite );
                Console.WriteLine( $"##teamcity[testSuiteFinished name='{suite.Name}']" );
            }
        }

        private static void ParseTest( string line )
        {
            var testLine = JsonConvert.DeserializeObject<TestLine>( line );
            _testNames.Add( testLine.test.id, testLine.test.name );
            Console.WriteLine( $"##teamcity[testStarted name='{testLine.test.name}']" );
        }

        private static void ParseGroup( string line )
        {
            var groupLine = JsonConvert.DeserializeObject<GroupLine>( line );
            var groupName = groupLine.group.name ?? "tests.dart";

            Console.WriteLine( $"##teamcity[testSuiteStarted name='{groupName}']" );

            _suites.Add( new Suite
            {
                GroupId = groupLine.group.id,
                Name = groupName,
                TestsRemaining = groupLine.group.testCount
            } );
        }
    }

    public class GroupLine
    {
        public Group group { get; set; }
    }

    public class Group
    {
        public int id { get; set; }
        public string name { get; set; }
        public int testCount { get; set; }
    }

    public class TestLine
    {
        public Test test { get; set; }
    }

    public class Test
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class TestId
    {
        public int testID { get; set; }
        public string result { get; set; }
        public int time { get; set; }
    }

    public class ArgumentsParser
    {
        public Dictionary<string, string> Parse( params string[] arguments )
        {
            var result = new Dictionary<string, string>();

            if ( arguments.Length == 0 )
                return result;

            var startIndex = arguments.First().StartsWith( "-" ) ? 0 : 1;
            var length = arguments.Length % 2 == 0 ? arguments.Length : arguments.Length - 1;

            for ( int i = startIndex; i < startIndex + length; i += 2 )
                result.Add( arguments[ i ].Substring( 1 ), arguments[ i + 1 ] );

            if ( arguments.Length % 2 == 1 )
            {
                var value = arguments.First().StartsWith( "-" ) ? arguments[ arguments.Length - 1 ] : arguments[ 0 ];
                result.Add( "", value );
            }

            return result;
        }
    }
}
