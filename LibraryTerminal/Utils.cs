using System;
using Microsoft.SPOT;

namespace LibraryTerminal
{
    class Utils
    {
        /// <summary>
        /// Converts the string representation of a date in the format "yyyy-MM-ddTHH:mm:ss" to its DateTime equivalent.
        /// </summary>
        /// <param name="s">The string representation of a date in the format "yyyy-MM-ddTHH:mm:ss"</param>
        /// <returns>The converted DateTime </returns>
        /// <exception cref="ArgumentNullException">Thrown when the string paramiter is null</exception>
        /// <exception cref="ArgumentException">Thrown when the string paramiter is not in the correct format</exception>       
        public static DateTime Parse(string s)
        {
            if (s == null) throw new ArgumentNullException();
            string[] tokens = s.Split('-','+', 'T', ':');
            if (tokens.Length < 6) throw new ArgumentException("Invalid format");
            try
            {
                int year = System.Math.Max(Int32.Parse(tokens[0]),1601);
                int month = Int32.Parse(tokens[1]);
                int day = Int32.Parse(tokens[2]);
                int hour = Int32.Parse(tokens[3]);
                int minute = Int32.Parse(tokens[4]);
                int second = Int32.Parse(tokens[5]);
                return new DateTime(year, month, day, hour, minute, second);
            }
            catch (Exception e) { throw new ArgumentException("Invalid format",e); }          
        }
    }

}
