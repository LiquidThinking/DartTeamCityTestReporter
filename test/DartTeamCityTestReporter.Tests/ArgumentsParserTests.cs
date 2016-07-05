using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using DartTeamCityTestReporter;

namespace DartTeamCityTestReporter.Tests
{
    public class ArgumentsParserTests
    {
        [Fact]
        public void CanParseNoArguments()
        {
            var parsedArguments = new ArgumentsParser().Parse();

            Assert.Equal( parsedArguments.Count, 0 );
        }

        [Fact]
        public void CanParseSingleNonNamedParameter()
        {
            var parsedArguments = new ArgumentsParser().Parse( "myfile.dart" );

            Assert.Equal( "myfile.dart", parsedArguments[""] );
        }

        [Fact]
        public void CanParseSingleNonNamedParameterWithNamedParameter()
        {
            var parsedArguments = new ArgumentsParser().Parse( "-dart", "dart.exe", "myfile.dart" );

            Assert.Equal( "myfile.dart", parsedArguments[""] );
            Assert.Equal( "dart.exe", parsedArguments["dart"] );
        }

        [Fact]
        public void CanParseSingleNonNamedParameterWithNamedParameterAfter()
        {
            var parsedArguments = new ArgumentsParser().Parse( "myfile.dart", "-dart", "dart.exe" );

            Assert.Equal( "myfile.dart", parsedArguments[""] );
            Assert.Equal( "dart.exe", parsedArguments["dart"] );
        }
    }
}
