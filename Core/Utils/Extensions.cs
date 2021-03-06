///
/// WF.Player.Core - A Wherigo Player Core for different platforms.
/// Copyright (C) 2012-2014  Dirk Weltz <web@weltz-online.de>
/// Copyright (C) 2012-2014  Brice Clocher <contact@cybisoft.net>
///
/// This program is free software: you can redistribute it and/or modify
/// it under the terms of the GNU Lesser General Public License as
/// published by the Free Software Foundation, either version 3 of the
/// License, or (at your option) any later version.
/// 
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
/// GNU Lesser General Public License for more details.
/// 
/// You should have received a copy of the GNU Lesser General Public License
/// along with this program.  If not, see <http://www.gnu.org/licenses/>.
///

using System.Text;
using System;
using System.Text.RegularExpressions;

namespace WF.Player.Core.Utils
{
	/// <summary>
	/// Adds various useful extensions to common types used by the core.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Returns a new string in which all occurrences of a specified string 
		/// in the current instance are replaced with another specified string.
		/// </summary>
		/// <param name="str">String to perform the replacement in.</param>
		/// <param name="oldValue">The string to be replaced. </param>
		/// <param name="newValue">The string to replace all occurrences of <paramref name="oldValue"/>. </param>
		/// <param name="comparison">The kind of comparison to perform.</param>
		/// <returns>A string that is equivalent to the current string except that all instances of <paramref name="oldValue"/>
		/// that match the <paramref name="comparison"/> type are replaced with <paramref name="newValue"/>.</returns>
		public static string Replace(this string str, string oldValue, string newValue, StringComparison comparison)
		{
			StringBuilder sb = new StringBuilder();

			int previousIndex = 0;
			int index = str.IndexOf(oldValue, comparison);
			while (index != -1)
			{
				sb.Append(str.Substring(previousIndex, index - previousIndex));
				sb.Append(newValue);
				index += oldValue.Length;

				previousIndex = index;
				index = str.IndexOf(oldValue, index, comparison);
			}
			sb.Append(str.Substring(previousIndex));

			return sb.ToString();
		}

		/// <summary>
		/// Returns a new string in which all matches of a specified regular expression 
		/// in the current instance are replaced with another specified string.
		/// </summary>
		/// <param name="str">String to perform the replacement in.</param>
		/// <param name="regex">The regular expression to match for. </param>
		/// <param name="newValue">The string to replace all matches of <paramref name="regex"/>. </param>
		/// <param name="regexOptions">The combined options for regular expression matching.</param>
		/// <returns>A string that is equivalent to the current string except that all matches of <paramref name="regex"/>
		/// that match the <paramref name="regexOptions"/> type are replaced with <paramref name="newValue"/>.</returns>
		public static string Replace(this string str, string regex, string newValue, RegexOptions regexOptions)
		{
			return Regex.Replace(str, regex, newValue, regexOptions);
		}

		/// <summary>
		/// Returns an array that is the copy of this array, prefixed by an element.
		/// </summary>
		/// <typeparam name="T">Type of the array.</typeparam>
		/// <param name="array">Base array.</param>
		/// <param name="obj">Object that will become the first element of the new array.</param>
		/// <returns>A new Array whose first element is <paramref name="obj"/> and then contains
		/// all elements of <paramref name="array"/> in their original order.</returns>
		public static T[] ConcatBefore<T>(this T[] array, T obj)
		{
			// Creates a new array with the specified first element.
			T[] newArray = new T[] { obj };

			// Copies the rest of the array.
			if (array.Length > 0)
			{
				Array.Resize(ref newArray, array.Length + 1);
				Array.Copy(array, 0, newArray, 1, array.Length);
			}

			return newArray;
		}

		/// <summary>
		/// Cleans a string from specific markup by converting it to its equivalent
		/// values in the environment.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		internal static string ReplaceHTMLMarkup(this string s)
		{
			// <BR> and <BR/> and <BR>\n -> new line
			// &nbsp; and &nbsp; + space -> space
			// &lt; -> '<'
			// &gt; -> '>'
			// &amp; and &amp;&amp; -> &
			// \n -> Environment.NewLine

			if (s == null)
			{
				return null;
			}

			// Defines the options for replacement: ignore case and culture invariant.
			System.Text.RegularExpressions.RegexOptions ro = System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant;
			StringComparison sc = StringComparison.InvariantCultureIgnoreCase;

			return s
				.Replace("<BR/?>\n?", "\n", ro)
				.Replace("(?:&nbsp; ?)+", " ", ro)
				.Replace("&lt;", "<", sc)
				.Replace("&gt;", ">", sc)
				.Replace("(?:&amp;)+", "&", ro)
				.Replace("\n", Environment.NewLine, sc);
		}

		/// <summary>
		/// Replaces all script markup with textual equivalents.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		internal static string ReplaceHTMLScriptMarkup(this string s)
		{
			// '<' -> &lt; 
			// '>' -> &gt;

			if (s == null)
			{
				return null;
			}

			// Defines the options for replacement: ignore case and culture invariant.
			StringComparison sc = StringComparison.InvariantCultureIgnoreCase;

			return s
					.Replace("<", "&lt;", sc)
					.Replace(">", "&gt;", sc);
		}

		/// <summary>
		/// Replaces all Markdown markup.
		/// </summary>
		/// <param name="s"></param>
		/// <param name="markdown"></param>
		/// <returns></returns>
		internal static string ReplaceMarkdown(this string s)
		{
			return new MarkdownSharp.Markdown().Transform(s);
		}
	}
}
