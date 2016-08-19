using System.Collections.Generic;
using System.Linq;

namespace DartTeamCityTestReporter
{
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